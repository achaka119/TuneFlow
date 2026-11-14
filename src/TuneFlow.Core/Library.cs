namespace TuneFlow.Core;
public sealed record Track(int Id, string Title, string Artist, string Genre, string Mood, int Bpm, string Color, int Duration = 32, string? LocalAudioKey = null) { public string Audio => LocalAudioKey is null ? $"/audio/{Id}.wav" : $"local:{LocalAudioKey}"; }
public sealed record Playlist(Guid Id, string Name, List<int> TrackIds);
public sealed record LibrarySnapshot(int Version, int[] Favorites, Playlist[] Playlists, int[] History, Track[]? Uploads = null);
public static class Catalog {
 public static readonly IReadOnlyList<Track> Tracks = Array.AsReadOnly(new[] {
 new Track(1,"Midnight Drive","TuneFlow Studio","Electronic","Energy",112,"#b6a0ff"),
 new Track(2,"Soft Horizons","TuneFlow Studio","Ambient","Focus",72,"#f2b48b"),
 new Track(3,"After the Rain","TuneFlow Studio","Lo-fi","Relax",84,"#83bba7"),
 new Track(4,"Neon Pulse","TuneFlow Studio","Electronic","Energy",128,"#f099b5"),
 new Track(5,"Quiet Orbit","TuneFlow Studio","Ambient","Focus",64,"#92b4df"),
 new Track(6,"Sunday Coffee","TuneFlow Studio","Lo-fi","Relax",90,"#d5c987") });
 public static IEnumerable<Track> Search(string? query, string genre = "All", IEnumerable<Track>? source = null) => (source ?? Tracks).Where(t => (genre == "All" || t.Genre == genre) && (string.IsNullOrWhiteSpace(query) || $"{t.Title} {t.Artist} {t.Genre} {t.Mood}".Contains(query.Trim(),StringComparison.OrdinalIgnoreCase)));
 // Pure ranking: same inputs always produce the same ordering.
 public static IReadOnlyList<Track> Mix(string mood, IEnumerable<int> favorites, IEnumerable<Track>? source = null) {
 var liked = favorites.ToHashSet();
 return (source ?? Tracks).OrderByDescending(t => (t.Mood == mood ? 10 : 0) + (liked.Contains(t.Id) ? 2 : 0)).ThenBy(t => t.Id).ToArray();
 }
}
public sealed class LibraryState {
 readonly List<Track> uploads = new();
 public IReadOnlyList<Track> Tracks => Catalog.Tracks.Concat(uploads).ToArray();
 public int NextUploadId => Math.Max(100000, uploads.Select(t=>t.Id).DefaultIfEmpty(99999).Max()+1);
 public static void ValidateUploadText(string title,string artist) {
 if(string.IsNullOrWhiteSpace(title)||title.Trim().Length>100)throw new ArgumentException("Enter a song title (1–100 characters).");
 if(string.IsNullOrWhiteSpace(artist)||artist.Trim().Length>100)throw new ArgumentException("Enter an artist (1–100 characters).");
 }
 static bool ValidUpload(Track t) => t.Id>=100000 && t.Id<int.MaxValue && !string.IsNullOrWhiteSpace(t.Title) && t.Title.Length<=100 && !string.IsNullOrWhiteSpace(t.Artist) && t.Artist.Length<=100 && t.Duration>0 && t.Duration<=86400 && t.Genre=="Uploads" && t.Mood=="Personal" && t.Color=="#b6a0ff" && Guid.TryParseExact(t.LocalAudioKey,"N",out _);
 public void RegisterUpload(Track track) {
 ValidateUploadText(track.Title,track.Artist);
 if(!ValidUpload(track))throw new ArgumentException("Invalid uploaded track.");
 if(uploads.Count>=50)throw new ArgumentException("You can import up to 50 songs.");
 if(Tracks.Any(t=>t.Id==track.Id)||uploads.Any(t=>t.LocalAudioKey==track.LocalAudioKey))throw new ArgumentException("This song is already imported.");
 uploads.Add(track with {Title=track.Title.Trim(),Artist=track.Artist.Trim()});
 }
 readonly HashSet<int> favorites = new(); readonly List<Playlist> playlists = new(); readonly List<int> history = new();
 public IReadOnlySet<int> Favorites => new HashSet<int>(favorites);
 public IReadOnlyList<Playlist> Playlists => playlists.Select(x=>x with { TrackIds = x.TrackIds.ToList() }).ToArray();
 public IReadOnlyList<int> History => history.ToArray();
 bool Valid(int id) => Tracks.Any(t=>t.Id==id);
 public void ToggleFavorite(int id) { if(!Valid(id)) throw new ArgumentException("Unknown track"); if(!favorites.Remove(id)) favorites.Add(id); }
 public Playlist Create(string name) { name = name.Trim(); if(name.Length is <1 or >40) throw new ArgumentException("Use a playlist name between 1 and 40 characters."); if(playlists.Count>=50) throw new ArgumentException("Playlist limit reached (50)."); if(playlists.Any(p=>p.Name.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("This playlist name already exists."); var p=new Playlist(Guid.NewGuid(),name,new());playlists.Add(p);return p with { TrackIds = new() }; }
 public void Rename(Guid id,string name) { name=name.Trim();if(name.Length is <1 or >40) throw new ArgumentException("Use 1–40 characters.");if(playlists.Any(p=>p.Id!=id && p.Name.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("This name already exists.");var i=playlists.FindIndex(p=>p.Id==id);if(i<0)throw new ArgumentException("Playlist not found.");playlists[i]=playlists[i] with {Name=name}; }
 public void Delete(Guid id) => playlists.RemoveAll(p=>p.Id==id);
 public void Add(Guid id,int track) {if(!Valid(track))throw new ArgumentException("Unknown track");var p=playlists.FirstOrDefault(p=>p.Id==id) ?? throw new ArgumentException("Playlist not found");if(!p.TrackIds.Contains(track))p.TrackIds.Add(track);}
 public void Remove(Guid id,int track) => playlists.FirstOrDefault(p=>p.Id==id)?.TrackIds.Remove(track);
 public void Record(int id) {if(!Valid(id))return;history.Remove(id);history.Insert(0,id);if(history.Count>30)history.RemoveAt(30);}
 public LibrarySnapshot Snapshot() => new(1,favorites.Order().ToArray(),Playlists.ToArray(),history.ToArray(),uploads.ToArray());
 public void Restore(LibrarySnapshot data) {
 if(data.Version!=1)throw new ArgumentException("Unsupported library version.");
 uploads.Clear();uploads.AddRange((data.Uploads??Array.Empty<Track>()).Where(t=>t!=null && ValidUpload(t)).GroupBy(t=>t.Id).Select(g=>g.First()).GroupBy(t=>t.LocalAudioKey).Select(g=>g.First()).Take(50));
 var clean=(data.Playlists??Array.Empty<Playlist>()).Take(50).Where(p=>p!=null && !string.IsNullOrWhiteSpace(p.Name) && p.Name.Trim().Length<=40).GroupBy(p=>p.Id).Select(g=>g.First()).GroupBy(p=>p.Name.Trim(),StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).Select(p=>p with {Name=p.Name.Trim(),TrackIds=(p.TrackIds??new()).Where(Valid).Distinct().ToList()}).ToArray();
 favorites.Clear();favorites.UnionWith((data.Favorites??Array.Empty<int>()).Where(Valid));playlists.Clear();playlists.AddRange(clean);history.Clear();history.AddRange((data.History??Array.Empty<int>()).Where(Valid).Distinct().Take(30));
 }
}
