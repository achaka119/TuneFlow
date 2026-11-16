# TuneFlow — A Music Streaming Application

**Askar Kairatbek · IT-2501**
**Tolbasy Aliakbar · IT-2501**

An academic C# / ASP.NET Core Blazor application with a working audio player, a searchable original music catalog, favorites, playlists, history and an explainable mood mix.

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
dotnet run --project tests/TuneFlow.RenderChecks --disable-build-servers
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

- src/TuneFlow.Core: domain records, handwritten catalog algorithms, Result<T> and validated application state.
- src/TuneFlow.Web: host, APIs, Razor components, CSS and a small HTML audio bridge.
- tests: domain, HTTP and optional end-to-end suites.

## Scope and limitations

This is a working academic demo, not a commercial catalog service. All six 32-second tracks are original synthesized demo compositions generated for this project. No external songs or commercial artwork are included. Audio is streamed from local static files using HTTP byte ranges, not adaptive HLS/DASH. The app needs a running server and an active Blazor connection. Personal data lives in browser localStorage, not a database. There is no login, paid subscription or cross-device synchronization. Personal audio import is available in Search. Do not deploy as a public multi-user service without authentication, HTTPS, durable storage, operational monitoring and load testing.

The project targets .NET 8 because that SDK is installed in the working environment. Check the official .NET support policy before future deployment and retarget to a supported SDK when necessary. No grade or complete cross-platform test coverage is claimed.

See docs/QA.md for what was actually verified and what remains unverified.

## Import personal songs

Search → Upload song → enter Song title and Artist → Choose File → Import song. Supported extensions: MP3, WAV, M4A and OGG. The browser must support the codec. Limit: 20 MB per file and 50 imported songs. Invalid, empty, oversized and unplayable files show an error. The duration is read from audio metadata.

## Downloads

- [Mac application ZIP](downloads/TuneFlow-Mac.zip): Apple Silicon, macOS 13+, installed .NET 8 runtime required. Extract, then double-click TuneFlow-Mac.app.


## C# implementation

`Library.cs` has been removed. Each model now has its own file in `src/TuneFlow.Core/Models`. `Catalog.cs` implements filtering with foreach and ranking with insertion sort. `LibraryState.cs` implements song and playlist lookup, duplicate handling, history, defensive copying and snapshot restoration with explicit loops. `Result.cs` contains our own generic success/failure type, including Map and Bind. Forms use it to show validation errors.

The application code does not use LINQ. Its implicit System.Linq import is disabled in both application projects. Standard .NET collections, JSON serialization and the ASP.NET Core/Blazor framework remain in use. The tests intentionally use LINQ as an independent reference to compare the handwritten algorithms.

C# event handlers are in `Home.razor.cs` and `UploadPanel.razor.cs`; their corresponding `.razor` files contain markup. Existing version-1 browser snapshots remain compatible. Playlist display and playback follow the same track-addition order.
