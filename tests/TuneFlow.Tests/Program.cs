using TuneFlow.Core;
using System.Text.Json;
int passed=0,failed=0;
void Test(string name,Action f){try{f();Console.WriteLine($"PASS {name}");passed++;}catch(Exception e){Console.WriteLine($"FAIL {name}: {e.Message}");failed++;}}
void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
void Reject(Action f){try{f();}catch(ArgumentException){return;}throw new Exception("Expected validation error");}
Test("Search ignores case and trims query",()=>Check(Catalog.Search("  MIDNIGHT  ").Single().Id==1));
Test("Search combines genre and query",()=>Check(!Catalog.Search("Midnight","Ambient").Any()));
Test("Empty search returns all tracks",()=>Check(Catalog.Search("").Count()==6));
Test("Unknown query returns no tracks",()=>Check(!Catalog.Search("unknown").Any()));
Test("Mix prioritizes selected mood",()=>Check(Catalog.Mix("Focus",Array.Empty<int>()).Take(2).All(t=>t.Mood=="Focus")));
Test("Mix uses favorite preference within mood",()=>Check(Catalog.Mix("Focus",new[]{5})[0].Id==5));
Test("Mix deterministic and unique",()=>Check(Catalog.Mix("Relax",new[]{3}).Select(t=>t.Id).SequenceEqual(Catalog.Mix("Relax",new[]{3}).Select(t=>t.Id)) && Catalog.Mix("Relax",new[]{3}).Select(t=>t.Id).Distinct().Count()==6));
Test("Favorite toggle is reversible",()=>{var s=new LibraryState();s.ToggleFavorite(1);Check(s.Favorites.Contains(1));s.ToggleFavorite(1);Check(!s.Favorites.Any());});
Test("Reject invalid track favorite",()=>Reject(()=>new LibraryState().ToggleFavorite(999)));
Test("Reject blank, long and duplicate playlist names",()=>{var s=new LibraryState();Reject(()=>s.Create("  "));Reject(()=>s.Create(new string('a',41)));s.Create("Focus");Reject(()=>s.Create(" focus "));});
Test("Playlist create, add, deduplicate, remove, delete",()=>{var s=new LibraryState();var p=s.Create("Mix");s.Add(p.Id,1);s.Add(p.Id,1);Check(s.Playlists[0].TrackIds.Count==1);s.Remove(p.Id,1);Check(s.Playlists[0].TrackIds.Count==0);s.Delete(p.Id);Check(s.Playlists.Count==0);});
Test("Rename validates duplicate",()=>{var s=new LibraryState();var p=s.Create("One");s.Create("Two");Reject(()=>s.Rename(p.Id,"Two"));s.Rename(p.Id,"New");Check(s.Playlists[0].Name=="New");});
Test("Adding invalid track rejects without mutation",()=>{var s=new LibraryState();var p=s.Create("Mix");Reject(()=>s.Add(p.Id,999));Check(s.Playlists[0].TrackIds.Count==0);});
Test("History is recent first and unique",()=>{var s=new LibraryState();s.Record(1);s.Record(2);s.Record(1);s.Record(999);Check(s.History.SequenceEqual(new[]{1,2}));});
Test("JSON snapshot roundtrip",()=>{var s=new LibraryState();s.ToggleFavorite(2);var p=s.Create("Study");s.Add(p.Id,5);s.Record(3);var t=new LibraryState();t.Restore(JsonSerializer.Deserialize<LibrarySnapshot>(JsonSerializer.Serialize(s.Snapshot()))!);Check(t.Favorites.Contains(2)&&t.Playlists[0].TrackIds.Contains(5)&&t.History[0]==3);});
Test("Restore sanitizes unknown and duplicate tracks",()=>{var s=new LibraryState();s.Restore(new(1,new[]{1,999,1},new[]{new Playlist(Guid.NewGuid(),"Good",new(){2,2,999})},new[]{3,3,999}));Check(s.Favorites.Count==1&&s.Playlists[0].TrackIds.SequenceEqual(new[]{2})&&s.History.SequenceEqual(new[]{3}));});
Test("Unsupported restore version preserves data",()=>{var s=new LibraryState();s.ToggleFavorite(1);Reject(()=>s.Restore(new(2,[],[],[])));Check(s.Favorites.Contains(1));});
Test("Snapshot and public playlist data are isolated",()=>{var s=new LibraryState();var p=s.Create("Mix");s.Add(p.Id,1);s.Snapshot().Playlists[0].TrackIds.Clear();s.Playlists[0].TrackIds.Clear();Check(s.Playlists[0].TrackIds.Count==1);});
Test("Playlist count capped",()=>{var s=new LibraryState();for(var i=0;i<50;i++)s.Create($"List {i}");Reject(()=>s.Create("Overflow"));});
Track Upload(int id=100000) => new(id,"Test song","Test artist","Uploads","Personal",0,"#b6a0ff",180,Guid.NewGuid().ToString("N"));
Test("Uploaded tracks participate in search, favorites and playlists",()=>{var s=new LibraryState();var t=Upload();s.RegisterUpload(t);Check(Catalog.Search("test artist","All",s.Tracks).Single().Id==t.Id);s.ToggleFavorite(t.Id);var p=s.Create("Imported");s.Add(p.Id,t.Id);s.Record(t.Id);Check(s.Favorites.Contains(t.Id)&&s.Playlists[0].TrackIds.Contains(t.Id)&&s.History[0]==t.Id);});
Test("Uploaded snapshot restores references before favorite validation",()=>{var s=new LibraryState();var t=Upload();s.RegisterUpload(t);s.ToggleFavorite(t.Id);var p=s.Create("Local");s.Add(p.Id,t.Id);var restored=new LibraryState();restored.Restore(JsonSerializer.Deserialize<LibrarySnapshot>(JsonSerializer.Serialize(s.Snapshot()))!);Check(restored.Tracks.Count==7&&restored.Favorites.Contains(t.Id)&&restored.Playlists[0].TrackIds.Contains(t.Id));});
Test("Invalid upload text, ID, duration and source are rejected",()=>{var s=new LibraryState();var t=Upload();Reject(()=>s.RegisterUpload(t with {Title=" "}));Reject(()=>s.RegisterUpload(t with {Artist=new string('x',101)}));Reject(()=>s.RegisterUpload(t with {Id=1}));Reject(()=>s.RegisterUpload(t with {Duration=0}));Reject(()=>s.RegisterUpload(t with {LocalAudioKey="../../bad"}));Check(s.Tracks.Count==6);});
Test("Upload restore filters invalid records and duplicate IDs",()=>{var s=new LibraryState();var t=Upload();s.Restore(new(1,[],[],[],new[]{t,t,t with {Id=100001,LocalAudioKey="https://example.com"}}));Check(s.Tracks.Count==7&&s.NextUploadId==100001);});
Test("Legacy snapshot remains compatible",()=>{var s=new LibraryState();s.Restore(new(1,new[]{1},[],[]));Check(s.Tracks.Count==6&&s.Favorites.Contains(1));});
Test("Mix includes uploaded songs without changing original catalog",()=>{var s=new LibraryState();s.RegisterUpload(Upload());Check(Catalog.Mix("Focus",[],s.Tracks).Count==7&&Catalog.Tracks.Count==6);});

// LINQ is used only here as an independent reference for the handwritten algorithms.
Test("Manual search matches reference across queries and genres", () =>
{
    string?[] queries = { null, "", "  ", "MIDNIGHT", "focus", "lo-fi", "Drive TuneFlow", "missing" };
    string[] genres = { "All", "Ambient", "Electronic", "Lo-fi", "Uploads", "missing" };
    foreach (string? query in queries)
    foreach (string genre in genres)
    {
        int[] expected = Catalog.Tracks.Where(t => (genre == "All" || t.Genre == genre) &&
            (string.IsNullOrWhiteSpace(query) || $"{t.Title} {t.Artist} {t.Genre} {t.Mood}".Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.Id).ToArray();
        Check(Catalog.Search(query, genre).Select(t => t.Id).SequenceEqual(expected));
    }
});
Test("Insertion sort matches reference for shuffled catalogs", () =>
{
    Random random = new Random(42);
    for (int trial = 0; trial < 100; trial++)
    {
        List<Track> source = Catalog.Tracks.OrderBy(_ => random.Next()).ToList();
        int[] favorites = Catalog.Tracks.Where(_ => random.Next(2) == 0).Select(t => t.Id).ToArray();
        string mood = new[] { "Focus", "Relax", "Energy", "Unknown" }[trial % 4];
        int[] before = source.Select(t => t.Id).ToArray();
        int[] expected = source.OrderByDescending(t => (t.Mood == mood ? 10 : 0) + (favorites.Contains(t.Id) ? 2 : 0))
            .ThenBy(t => t.Id).Select(t => t.Id).ToArray();
        Check(Catalog.Mix(mood, favorites, source).Select(t => t.Id).SequenceEqual(expected));
        Check(source.Select(t => t.Id).SequenceEqual(before));
    }
});
Test("Mix handles empty and single-song sources", () =>
{
    Check(Catalog.Mix("Focus", new[] { 1 }, Array.Empty<Track>()).Count == 0);
    Check(Catalog.Mix("Focus", new[] { 1 }, new[] { Catalog.Tracks[0] })[0].Id == 1);
});
Test("Repeated favorite IDs do not inflate a score", () => Check(Catalog.Score(Catalog.Tracks[0], "Energy", new[] { 1, 1 }) == 12));
Test("FirstTracks handles zero, negative and overlarge limits", () =>
{
    Check(Catalog.FirstTracks(Catalog.Tracks, 0).Count == 0);
    Check(Catalog.FirstTracks(Catalog.Tracks, -1).Count == 0);
    Check(Catalog.FirstTracks(Catalog.Tracks, 99).Count == 6);
    Check(Catalog.FirstTracks(Catalog.Tracks, 2).Select(t => t.Id).SequenceEqual(new[] { 1, 2 }));
});
Test("Manual queue lookup handles missing and present songs", () =>
{
    Check(Catalog.FindTrackIndex(Catalog.Tracks, 1) == 0);
    Check(Catalog.FindTrackIndex(Catalog.Tracks, 6) == 5);
    Check(Catalog.FindTrackIndex(Catalog.Tracks, 99) == -1);
});
Test("Catalog copy does not expose the source collection", () =>
{
    List<Track> copy = Catalog.CopyTracks(Catalog.Tracks);
    copy.Clear();
    Check(Catalog.Tracks.Count == 6);
});
Test("Result Map transforms success", () => Check(Result<int>.Success(4).Map(x => x * 3).Value == 12));
Test("Result Bind composes successful operations", () => Check(Result<int>.Success(4).Bind(x => Result<string>.Success($"song-{x}")).Value == "song-4"));
Test("Result Map and Bind short-circuit failures", () =>
{
    bool called = false;
    Result<int> mapped = Result<int>.Failure("invalid").Map(x => { called = true; return x + 1; });
    Result<int> bound = mapped.Bind(x => { called = true; return Result<int>.Success(x); });
    Check(!called && bound.IsFailure && bound.Error == "invalid");
});
Test("Result Bind preserves an error from the next operation", () =>
{
    Result<int> result = Result<int>.Success(1).Bind(_ => Result<int>.Failure("next failed"));
    Check(result.IsFailure && result.Error == "next failed");
});
Test("Failed Result cannot expose a default value", () =>
{
    try { _ = Result<int>.Failure("invalid").Value; }
    catch (InvalidOperationException) { return; }
    throw new Exception("Expected invalid value access to fail");
});
Test("Result requires an error message", () => Reject(() => Result<int>.Failure(" ")));
Test("Playlist result validation has no mutation on failure", () =>
{
    LibraryState state = new LibraryState();
    Check(state.TryCreate(" ").IsFailure && state.Playlists.Count == 0);
    Playlist playlist = state.TryCreate(" Study ").Value;
    Check(playlist.Name == "Study");
    Check(state.TryCreate("study").IsFailure && state.Playlists.Count == 1);
    Check(state.TryRename(playlist.Id, " ").IsFailure && state.Playlists[0].Name == "Study");
    Check(state.TryRename(Guid.NewGuid(), "Other").IsFailure);
});
Test("Upload text validation trims both values through Map and Bind", () =>
{
    Result<(string Title, string Artist)> result = LibraryState.CheckUploadText(" Title ", " Artist ");
    Check(result.IsSuccess && result.Value.Title == "Title" && result.Value.Artist == "Artist");
    Check(LibraryState.CheckUploadText(" ", "Artist").Error!.Contains("title"));
    Check(LibraryState.CheckUploadText("Title", " ").Error!.Contains("artist"));
});
Test("Playlist track lookup preserves the order of additions", () =>
{
    LibraryState state = new LibraryState();
    Playlist playlist = state.Create("Ordered");
    state.Add(playlist.Id, 6); state.Add(playlist.Id, 1); state.Add(playlist.Id, 3);
    Playlist saved = state.FindPlaylist(playlist.Id)!;
    Check(state.PlaylistTracks(saved).Select(t => t.Id).SequenceEqual(new[] { 6, 1, 3 }));
});
Test("Playlist lookup and creation return defensive copies", () =>
{
    LibraryState state = new LibraryState();
    Playlist created = state.Create("Isolated");
    created.TrackIds.Add(99);
    state.Add(created.Id, 1);
    state.FindPlaylist(created.Id)!.TrackIds.Clear();
    Check(state.FindPlaylist(created.Id)!.TrackIds.SequenceEqual(new[] { 1 }));
    Check(state.FindPlaylist(Guid.NewGuid()) == null);
});
Test("Favorite and history queries return the correct ordering", () =>
{
    LibraryState state = new LibraryState();
    state.ToggleFavorite(6); state.ToggleFavorite(2);
    state.Record(6); state.Record(1); state.Record(6);
    Check(state.FavoriteTracks().Select(t => t.Id).SequenceEqual(new[] { 2, 6 }));
    Check(state.RecentTracks().Select(t => t.Id).SequenceEqual(new[] { 6, 1 }));
});
Test("Snapshot sorts favorites and cannot mutate state", () =>
{
    LibraryState state = new LibraryState();
    state.ToggleFavorite(6); state.ToggleFavorite(2); state.ToggleFavorite(1);
    LibrarySnapshot snapshot = state.Snapshot();
    Check(snapshot.Favorites.SequenceEqual(new[] { 1, 2, 6 }));
    snapshot.Favorites[0] = 999;
    Check(state.IsFavorite(1) && !state.IsFavorite(999));
});
Test("Restore deduplicates playlist IDs, trimmed names and track references", () =>
{
    LibraryState state = new LibraryState();
    Guid id = Guid.NewGuid();
    state.Restore(new LibrarySnapshot(1, new[] { 1, 1, 999 }, new[]
    {
        new Playlist(id, " One ", new List<int> { 3, 3, 1, 999 }),
        new Playlist(id, "Other", new List<int> { 2 }),
        new Playlist(Guid.NewGuid(), "ONE", new List<int> { 6 }),
        new Playlist(Guid.NewGuid(), " ", new List<int>())
    }, new[] { 3, 3, 999, 1 }));
    Check(state.Playlists.Count == 1 && state.Playlists[0].Name == "One");
    Check(state.Playlists[0].TrackIds.SequenceEqual(new[] { 3, 1 }));
    Check(state.History.SequenceEqual(new[] { 3, 1 }));
});
Test("Restore handles explicit null collections from browser JSON", () =>
{
    LibraryState state = new LibraryState();
    LibrarySnapshot snapshot = JsonSerializer.Deserialize<LibrarySnapshot>("{\"Version\":1,\"Favorites\":null,\"Playlists\":null,\"History\":null,\"Uploads\":null}")!;
    state.Restore(snapshot);
    Check(state.Tracks.Count == 6 && state.Playlists.Count == 0 && state.History.Count == 0);
});
Test("Restore history is capped and recent recording keeps it capped", () =>
{
    Track[] uploads = Enumerable.Range(100000, 40).Select(id => Upload(id)).ToArray();
    LibraryState state = new LibraryState();
    state.Restore(new LibrarySnapshot(1, Array.Empty<int>(), Array.Empty<Playlist>(), uploads.Select(t => t.Id).ToArray(), uploads));
    Check(state.History.Count == 30);
    state.Record(100039);
    Check(state.History.Count == 30 && state.History[0] == 100039);
});
Test("Restore upload limit and next-ID scan remain valid", () =>
{
    Track[] uploads = Enumerable.Range(100000, 55).Select(id => Upload(id)).ToArray();
    LibraryState state = new LibraryState();
    state.Restore(new LibrarySnapshot(1, Array.Empty<int>(), Array.Empty<Playlist>(), Array.Empty<int>(), uploads));
    Check(state.UploadCount == 50 && state.NextUploadId == 100050);
    Reject(() => state.RegisterUpload(Upload(100060)));
});
Test("Duplicate audio keys do not create multiple uploaded tracks", () =>
{
    Track first = Upload();
    Track second = first with { Id = 100001 };
    LibraryState state = new LibraryState();
    state.Restore(new LibrarySnapshot(1, Array.Empty<int>(), Array.Empty<Playlist>(), Array.Empty<int>(), new[] { first, second }));
    Check(state.UploadCount == 1);
});
Test("Rejected restore preserves all previous state", () =>
{
    LibraryState state = new LibraryState();
    state.RegisterUpload(Upload()); state.ToggleFavorite(100000); state.Create("Keep");
    Reject(() => state.Restore(new LibrarySnapshot(9, Array.Empty<int>(), Array.Empty<Playlist>(), Array.Empty<int>())));
    Check(state.UploadCount == 1 && state.IsFavorite(100000) && state.Playlists[0].Name == "Keep");
});
Console.WriteLine($"\n{passed} passed, {failed} failed");return failed==0?0:1;
