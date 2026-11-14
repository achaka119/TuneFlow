# TuneFlow — A Music Streaming Application

**Askar Kairatbek · IT-2501**
**Tolbasy Aliakbar · IT-2501**

An academic C# / ASP.NET Core Blazor application with a working audio player, a searchable original music catalog, favorites, playlists, history and an explainable mood mix. The interface and defense materials are in English.

## Run

Install the .NET 8 SDK, then run from this folder:

```sh
dotnet run --project src/TuneFlow.Web --urls http://localhost:5080
```

Open http://localhost:5080. No database, API keys, Node.js or external NuGet packages are required. NuGet.Config intentionally clears external feeds because the project uses only the installed .NET shared frameworks. If running inside a restricted environment, use `./start.sh` (macOS/Linux) or `start.cmd` (Windows).

## Build and test

```sh
dotnet build TuneFlow.sln --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet run --project tests/TuneFlow.Tests --disable-build-servers
python3 tests/http_checks.py http://localhost:5080
```

The domain suite is a dependency-free executable test runner with a nonzero exit code on failure. It does not use `dotnet test`. The HTTP suite requires a running app. Optional browser automation is in tests/e2e (Playwright, see its README).

## Use

- Discover: choose Focus, Relax or Energy and press Play your mix.
- Search: filter by title, artist, genre or mood. Genre filters combine with the query.
- Player: play/pause, previous/next, seek, shuffle, repeat and queue. Volume and queue controls appear on wider layouts.
- Favorites: use the heart beside a track.
- Playlists: create a name, then use + beside a Search track. Open the playlist to play, rename, remove tracks or delete it after confirmation.
- History: recently started tracks, without duplicates.
- Reload: favorites, playlists and history persist in this browser and origin. Playback position and the active queue reset. Clearing browser storage removes the library.

## Project map

- src/TuneFlow.Core: domain records, pure LINQ catalog functions and validated library state.
- src/TuneFlow.Web: host, APIs, Razor components, CSS and a small HTML audio bridge.
- tests: domain, HTTP and optional end-to-end suites.
- docs: architecture, rubric mapping, QA results, screenshots and English defense script.

## Scope and limitations

This is a working academic demo, not a commercial catalog service. All six 32-second tracks are original synthesized demo compositions generated for this project. No external songs or commercial artwork are included. Audio is streamed from local static files using HTTP byte ranges, not adaptive HLS/DASH. The app needs a running server and an active Blazor connection. Personal data lives in browser localStorage, not a database. There is no login, paid subscription or cross-device synchronization. Personal audio import is available in Search. Do not deploy as a public multi-user service without authentication, HTTPS, durable storage, operational monitoring and load testing.

The project targets .NET 8 because that SDK is installed in the working environment. Check the official .NET support policy before future deployment and retarget to a supported SDK when necessary. No grade or complete cross-platform test coverage is claimed.

See docs/QA.md for what was actually verified and what remains unverified.

## Import personal songs

Search → Upload song → enter Song title and Artist → Choose File → Import song. Supported extensions: MP3, WAV, M4A and OGG. The browser must support the codec. Limit: 20 MB per file and 50 imported songs. Invalid, empty, oversized and unplayable files show an error. The duration is read from audio metadata.

Imported files stay in IndexedDB in the current browser and origin. Only metadata, favorites and playlists use localStorage. Audio bytes do not pass through the .NET server. Imported songs participate in search, favorites, playlists, history and the mix. The read-only HTTP catalog API intentionally continues to expose only public demo tracks. Files do not sync to another browser, hostname or port. Clearing site data, private browsing or storage eviction can remove them; keep the original files. No DRM-protected service downloads are supported.

## Downloads and defense materials

- [Mac application ZIP](downloads/TuneFlow-Mac.zip): Apple Silicon, macOS 13+, installed .NET 8 runtime required. Extract, then double-click TuneFlow-Mac.app. The launcher starts the server and opens the browser without Terminal.
- [English presentation](docs/TuneFlow-Presentation.pptx).
- [Defense script](docs/DEFENSE.md).
- [Assessment criteria mapping](docs/CRITERIA.md).

Build the Mac application from source with `sh tools/macos/build.sh` on an Apple Silicon Mac with the .NET 8 SDK and Apple command-line development tools. Generated output is placed in `artifacts/` and is not committed.
