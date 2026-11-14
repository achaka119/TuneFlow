#!/bin/sh
set -eu
ROOT_DIR=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
APP_DIR="$ROOT_DIR/artifacts/TuneFlow-Mac.app"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources/server"
dotnet publish "$ROOT_DIR/src/TuneFlow.Web/TuneFlow.Web.csproj" -c Release --self-contained false -p:UseAppHost=false -o "$APP_DIR/Contents/Resources/server"
cp "$ROOT_DIR/tools/macos/Info.plist" "$APP_DIR/Contents/Info.plist"
swiftc -target arm64-apple-macosx13.0 -framework Cocoa "$ROOT_DIR/tools/macos/TuneFlowLauncher.swift" -o "$APP_DIR/Contents/MacOS/TuneFlow"
if xattr -p com.apple.FinderInfo "$APP_DIR" >/dev/null 2>&1; then
 xattr -d com.apple.FinderInfo "$APP_DIR"
fi
codesign --force --deep --sign - "$APP_DIR"
printf 'Built %s\n' "$APP_DIR"
