import Cocoa
import Foundation

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var server: Process?
    private var window: NSWindow!
    private var status: NSTextField!
    private var detail: NSTextField!
    private var logHandle: FileHandle?
    private var logURL: URL?
    private let home = URL(string: "http://localhost:5080")!
    private var attempts = 0
    private var closing = false

    func applicationDidFinishLaunching(_ notification: Notification) {
        setupMenu()
        setupWindow()
        probe { running in
            if running { self.ready(existing: true) } else { self.startServer() }
        }
    }

    private func setupMenu() {
        let main = NSMenu()
        let root = NSMenuItem()
        let menu = NSMenu()
        menu.addItem(withTitle: "Open TuneFlow", action: #selector(openBrowser), keyEquivalent: "o").target = self
        menu.addItem(NSMenuItem.separator())
        menu.addItem(withTitle: "Quit TuneFlow", action: #selector(quit), keyEquivalent: "q").target = self
        root.submenu = menu
        main.addItem(root)
        NSApp.mainMenu = main
    }

    private func setupWindow() {
        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 460, height: 245), styleMask: [.titled, .closable, .miniaturizable], backing: .buffered, defer: false)
        window.title = "TuneFlow"
        window.center()
        let content = NSView(frame: NSRect(x: 0, y: 0, width: 460, height: 245))
        status = NSTextField(labelWithString: "Starting TuneFlow…")
        status.font = .systemFont(ofSize: 25, weight: .semibold)
        status.frame = NSRect(x: 30, y: 160, width: 400, height: 40)
        content.addSubview(status)
        detail = NSTextField(wrappingLabelWithString: "Your music will open in your browser automatically.")
        detail.font = .systemFont(ofSize: 13)
        detail.textColor = .secondaryLabelColor
        detail.frame = NSRect(x: 30, y: 86, width: 400, height: 65)
        content.addSubview(detail)
        let open = NSButton(title: "Open TuneFlow", target: self, action: #selector(openBrowser))
        open.bezelStyle = .rounded
        open.frame = NSRect(x: 25, y: 28, width: 150, height: 34)
        content.addSubview(open)
        let quit = NSButton(title: "Quit", target: self, action: #selector(quit))
        quit.bezelStyle = .rounded
        quit.frame = NSRect(x: 325, y: 28, width: 100, height: 34)
        content.addSubview(quit)
        window.contentView = content
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    private func probe(completion: @escaping (Bool) -> Void) {
        let url = home.appendingPathComponent("api/tracks")
        var request = URLRequest(url: url)
        request.timeoutInterval = 1
        URLSession.shared.dataTask(with: request) { data, response, _ in
            let tracks = data.flatMap { try? JSONSerialization.jsonObject(with: $0) as? [[String: Any]] }
            let valid = (response as? HTTPURLResponse)?.statusCode == 200 && tracks?.contains { ($0["title"] as? String) == "Midnight Drive" && ($0["artist"] as? String) == "TuneFlow Studio" } == true
            DispatchQueue.main.async { if !self.closing { completion(valid) } }
        }.resume()
    }

    private func startServer() {
        guard let resources = Bundle.main.resourceURL else { fail("Application files are missing."); return }
        let folder = resources.appendingPathComponent("server")
        let dll = folder.appendingPathComponent("TuneFlow.Web.dll")
        guard FileManager.default.fileExists(atPath: dll.path) else { fail("TuneFlow.Web.dll is missing. Keep the application bundle intact."); return }
        let candidates = ["/usr/local/share/dotnet/dotnet", "/usr/local/bin/dotnet", "/opt/homebrew/bin/dotnet", NSHomeDirectory()+"/.dotnet/dotnet"]
        guard let dotnet = candidates.first(where: { FileManager.default.isExecutableFile(atPath: $0) }) else {
            fail("The .NET 8 runtime is required. Install it from dotnet.microsoft.com, then reopen TuneFlow.")
            return
        }
        let p = Process()
        p.executableURL = URL(fileURLWithPath: dotnet)
        p.arguments = [dll.path, "--urls", home.absoluteString]
        p.currentDirectoryURL = folder
        var env = ProcessInfo.processInfo.environment
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        env["DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE"] = "false"
        env["ASPNETCORE_ENVIRONMENT"] = "Production"
        env["Logging__LogLevel__Default"] = "Warning"
        p.environment = env
        let log = FileManager.default.temporaryDirectory.appendingPathComponent("TuneFlow-\(UUID().uuidString).log")
        FileManager.default.createFile(atPath: log.path, contents: nil)
        do {
            logHandle = try FileHandle(forWritingTo: log)
            logURL = log
            p.standardOutput = logHandle
            p.standardError = logHandle
            p.terminationHandler = { [weak self] process in
                DispatchQueue.main.async {
                    guard let self = self, !self.closing else { return }
                    let text = self.logURL.flatMap { try? String(contentsOf: $0, encoding: .utf8) } ?? ""
                    self.fail("The server stopped (code \(process.terminationStatus)).\n" + String(text.suffix(1400)))
                }
            }
            server = p
            try p.run()
            waitUntilReady()
        } catch { fail("Could not start TuneFlow: \(error.localizedDescription)") }
    }

    private func waitUntilReady() {
        probe { running in
            if running { self.ready(existing: false); return }
            guard self.server?.isRunning == true else { return }
            self.attempts += 1
            if self.attempts >= 40 { self.fail("The server did not respond. Port 5080 may already be in use. Close the other server and reopen TuneFlow."); return }
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { if !self.closing { self.waitUntilReady() } }
        }
    }

    private func ready(existing: Bool) {
        status.stringValue = "TuneFlow is ready"
        detail.stringValue = existing ? "Connected to TuneFlow already running on this Mac.\nOpen Search → Upload song to add your music." : "Keep this app open while listening.\nOpen Search → Upload song to add your music."
        openBrowser()
    }

    private func fail(_ message: String) {
        status.stringValue = "Could not start TuneFlow"
        detail.stringValue = "Close this window and try again."
        let alert = NSAlert()
        alert.messageText = "TuneFlow could not start"
        alert.informativeText = message
        alert.alertStyle = .warning
        alert.addButton(withTitle: "OK")
        alert.runModal()
    }

    @objc private func openBrowser() { NSWorkspace.shared.open(home) }
    @objc private func quit() { NSApp.terminate(nil) }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        window.makeKeyAndOrderFront(nil)
        openBrowser()
        return true
    }
    func applicationWillTerminate(_ notification: Notification) {
        closing = true
        if let p = server, p.isRunning { p.terminate() }
        try? logHandle?.close()
        if let log = logURL { try? FileManager.default.removeItem(at: log) }
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.regular)
app.run()
