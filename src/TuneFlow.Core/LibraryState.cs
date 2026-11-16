namespace TuneFlow.Core;

// Scoped application state. All queries and validation are implemented here,
// with explicit loops instead of LINQ or an external data-management library.
public sealed class LibraryState
{
    private const int PlaylistLimit = 50;
    private const int UploadLimit = 50;
    private const int HistoryLimit = 30;
    private readonly List<Track> uploads = new List<Track>();
    private readonly HashSet<int> favorites = new HashSet<int>();
    private readonly List<Playlist> playlists = new List<Playlist>();
    private readonly List<int> history = new List<int>();

    public IReadOnlyList<Track> Tracks
    {
        get
        {
            List<Track> tracks = Catalog.CopyTracks(Catalog.Tracks);
            foreach (Track upload in uploads)
                tracks.Add(upload);
            return tracks;
        }
    }

    public int UploadCount { get { return uploads.Count; } }

    public int NextUploadId
    {
        get
        {
            int greatest = 99999;
            foreach (Track upload in uploads)
            {
                if (upload.Id > greatest)
                    greatest = upload.Id;
            }
            return checked(greatest + 1);
        }
    }

    public IReadOnlySet<int> Favorites
    {
        get
        {
            HashSet<int> copy = new HashSet<int>();
            foreach (int id in favorites)
                copy.Add(id);
            return copy;
        }
    }

    public IReadOnlyList<Playlist> Playlists
    {
        get
        {
            List<Playlist> copy = new List<Playlist>();
            foreach (Playlist playlist in playlists)
                copy.Add(CopyPlaylist(playlist));
            return copy;
        }
    }

    public IReadOnlyList<int> History { get { return CopyIds(history); } }

    // Map and Bind compose validation steps, as in the course Result examples.
    public static Result<(string Title, string Artist)> CheckUploadText(string title, string artist)
    {
        return CheckText(title, 100, "Enter a song title (1–100 characters).")
            .Bind(cleanTitle => CheckText(artist, 100, "Enter an artist (1–100 characters).")
                .Map(cleanArtist => (cleanTitle, cleanArtist)));
    }

    public static void ValidateUploadText(string title, string artist)
    {
        Result<(string Title, string Artist)> result = CheckUploadText(title, artist);
        if (result.IsFailure)
            throw new ArgumentException(result.Error);
    }

    private static Result<string> CheckText(string? text, int maxLength, string error)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<string>.Failure(error);

        string clean = text.Trim();
        if (clean.Length > maxLength)
            return Result<string>.Failure(error);

        return Result<string>.Success(clean);
    }

    private static bool ValidUpload(Track track)
    {
        return track.Id >= 100000 && track.Id < int.MaxValue &&
            !string.IsNullOrWhiteSpace(track.Title) && track.Title.Length <= 100 &&
            !string.IsNullOrWhiteSpace(track.Artist) && track.Artist.Length <= 100 &&
            track.Duration > 0 && track.Duration <= 86400 &&
            track.Genre == "Uploads" && track.Mood == "Personal" &&
            track.Color == "#b6a0ff" && Guid.TryParseExact(track.LocalAudioKey, "N", out _);
    }

    public void RegisterUpload(Track track)
    {
        ValidateUploadText(track.Title, track.Artist);
        if (!ValidUpload(track))
            throw new ArgumentException("Invalid uploaded track.");
        if (uploads.Count >= UploadLimit)
            throw new ArgumentException("You can import up to 50 songs.");

        foreach (Track upload in uploads)
        {
            if (upload.Id == track.Id || upload.LocalAudioKey == track.LocalAudioKey)
                throw new ArgumentException("This song is already imported.");
        }

        uploads.Add(track with { Title = track.Title.Trim(), Artist = track.Artist.Trim() });
    }

    public Track? FindTrack(int id)
    {
        foreach (Track track in Catalog.Tracks)
        {
            if (track.Id == id)
                return track;
        }
        foreach (Track track in uploads)
        {
            if (track.Id == id)
                return track;
        }
        return null;
    }

    public Playlist? FindPlaylist(Guid id)
    {
        int index = FindPlaylistIndex(id);
        if (index < 0)
            return null;
        return CopyPlaylist(playlists[index]);
    }

    private int FindPlaylistIndex(Guid id)
    {
        for (int i = 0; i < playlists.Count; i++)
        {
            if (playlists[i].Id == id)
                return i;
        }
        return -1;
    }

    public bool IsFavorite(int id) { return favorites.Contains(id); }

    public List<Track> FavoriteTracks()
    {
        List<Track> result = new List<Track>();
        foreach (Track track in Tracks)
        {
            if (favorites.Contains(track.Id))
                result.Add(track);
        }
        return result;
    }

    public List<Track> RecentTracks()
    {
        List<Track> result = new List<Track>();
        foreach (int id in history)
        {
            Track? track = FindTrack(id);
            if (track != null)
                result.Add(track);
        }
        return result;
    }

    public List<Track> PlaylistTracks(Playlist playlist)
    {
        List<Track> result = new List<Track>();
        foreach (int id in playlist.TrackIds)
        {
            Track? track = FindTrack(id);
            if (track != null)
                result.Add(track);
        }
        return result;
    }

    public void ToggleFavorite(int id)
    {
        if (FindTrack(id) == null)
            throw new ArgumentException("Unknown track");
        if (favorites.Contains(id))
            favorites.Remove(id);
        else
            favorites.Add(id);
    }

    private Result<string> CheckPlaylistName(string name, Guid? exceptId = null)
    {
        return CheckText(name, 40, "Use a playlist name between 1 and 40 characters.")
            .Bind(clean =>
            {
                foreach (Playlist playlist in playlists)
                {
                    if (playlist.Id != exceptId &&
                        playlist.Name.Equals(clean, StringComparison.OrdinalIgnoreCase))
                        return Result<string>.Failure("This playlist name already exists.");
                }
                return Result<string>.Success(clean);
            });
    }

    public Result<Playlist> TryCreate(string name)
    {
        if (playlists.Count >= PlaylistLimit)
            return Result<Playlist>.Failure("Playlist limit reached (50).");

        Result<string> validation = CheckPlaylistName(name);
        if (validation.IsFailure)
            return Result<Playlist>.Failure(validation.Error!);

        Playlist playlist = new Playlist(Guid.NewGuid(), validation.Value, new List<int>());
        playlists.Add(playlist);
        return Result<Playlist>.Success(CopyPlaylist(playlist));
    }

    public Playlist Create(string name)
    {
        Result<Playlist> result = TryCreate(name);
        if (result.IsFailure)
            throw new ArgumentException(result.Error);
        return result.Value;
    }

    public Result<Playlist> TryRename(Guid id, string name)
    {
        int index = FindPlaylistIndex(id);
        if (index < 0)
            return Result<Playlist>.Failure("Playlist not found.");

        Result<string> validation = CheckPlaylistName(name, id);
        if (validation.IsFailure)
            return Result<Playlist>.Failure(validation.Error!);

        playlists[index] = playlists[index] with { Name = validation.Value };
        return Result<Playlist>.Success(CopyPlaylist(playlists[index]));
    }

    public void Rename(Guid id, string name)
    {
        Result<Playlist> result = TryRename(id, name);
        if (result.IsFailure)
            throw new ArgumentException(result.Error);
    }

    public void Delete(Guid id)
    {
        int index = FindPlaylistIndex(id);
        if (index >= 0)
            playlists.RemoveAt(index);
    }

    public void Add(Guid id, int track)
    {
        if (FindTrack(track) == null)
            throw new ArgumentException("Unknown track");
        int index = FindPlaylistIndex(id);
        if (index < 0)
            throw new ArgumentException("Playlist not found");

        List<int> ids = playlists[index].TrackIds;
        foreach (int existing in ids)
        {
            if (existing == track)
                return;
        }
        ids.Add(track);
    }

    public void Remove(Guid id, int track)
    {
        int index = FindPlaylistIndex(id);
        if (index < 0)
            return;
        List<int> ids = playlists[index].TrackIds;
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == track)
            {
                ids.RemoveAt(i);
                return;
            }
        }
    }

    public void Record(int id)
    {
        if (FindTrack(id) == null)
            return;
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i] == id)
            {
                history.RemoveAt(i);
                break;
            }
        }
        history.Insert(0, id);
        if (history.Count > HistoryLimit)
            history.RemoveAt(HistoryLimit);
    }

    public LibrarySnapshot Snapshot()
    {
        int[] liked = CopyIds(favorites);
        // Insertion sort gives snapshots a stable favorite-ID order.
        for (int i = 1; i < liked.Length; i++)
        {
            int value = liked[i];
            int j = i - 1;
            while (j >= 0 && liked[j] > value)
            {
                liked[j + 1] = liked[j];
                j--;
            }
            liked[j + 1] = value;
        }

        Playlist[] playlistCopies = new Playlist[playlists.Count];
        for (int i = 0; i < playlists.Count; i++)
            playlistCopies[i] = CopyPlaylist(playlists[i]);
        Track[] uploadCopies = new Track[uploads.Count];
        for (int i = 0; i < uploads.Count; i++)
            uploadCopies[i] = uploads[i];

        return new LibrarySnapshot(1, liked, playlistCopies, CopyIds(history), uploadCopies);
    }

    public void Restore(LibrarySnapshot data)
    {
        if (data == null || data.Version != 1)
            throw new ArgumentException("Unsupported library version.");

        // Build clean temporary collections first; only then replace current state.
        List<Track> cleanUploads = new List<Track>();
        HashSet<int> uploadIds = new HashSet<int>();
        HashSet<string> audioKeys = new HashSet<string>();
        foreach (Track track in data.Uploads ?? Array.Empty<Track>())
        {
            if (track == null || !ValidUpload(track) || !uploadIds.Add(track.Id))
                continue;
            if (!audioKeys.Add(track.LocalAudioKey!))
                continue;
            cleanUploads.Add(track);
            if (cleanUploads.Count == UploadLimit)
                break;
        }

        bool IsKnownTrack(int id)
        {
            foreach (Track track in Catalog.Tracks)
            {
                if (track.Id == id)
                    return true;
            }
            foreach (Track track in cleanUploads)
            {
                if (track.Id == id)
                    return true;
            }
            return false;
        }

        List<Playlist> cleanPlaylists = new List<Playlist>();
        HashSet<Guid> playlistIds = new HashSet<Guid>();
        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int inspected = 0;
        foreach (Playlist playlist in data.Playlists ?? Array.Empty<Playlist>())
        {
            if (inspected++ == PlaylistLimit)
                break;
            if (playlist == null || string.IsNullOrWhiteSpace(playlist.Name))
                continue;
            string name = playlist.Name.Trim();
            if (name.Length > 40 || !playlistIds.Add(playlist.Id) || !names.Add(name))
                continue;

            List<int> cleanIds = new List<int>();
            HashSet<int> seen = new HashSet<int>();
            foreach (int id in playlist.TrackIds ?? new List<int>())
            {
                if (IsKnownTrack(id) && seen.Add(id))
                    cleanIds.Add(id);
            }
            cleanPlaylists.Add(new Playlist(playlist.Id, name, cleanIds));
        }

        HashSet<int> cleanFavorites = new HashSet<int>();
        foreach (int id in data.Favorites ?? Array.Empty<int>())
        {
            if (IsKnownTrack(id))
                cleanFavorites.Add(id);
        }

        List<int> cleanHistory = new List<int>();
        HashSet<int> historyIds = new HashSet<int>();
        foreach (int id in data.History ?? Array.Empty<int>())
        {
            if (!IsKnownTrack(id) || !historyIds.Add(id))
                continue;
            cleanHistory.Add(id);
            if (cleanHistory.Count == HistoryLimit)
                break;
        }

        uploads.Clear();
        foreach (Track track in cleanUploads)
            uploads.Add(track);
        playlists.Clear();
        foreach (Playlist playlist in cleanPlaylists)
            playlists.Add(playlist);
        favorites.Clear();
        foreach (int id in cleanFavorites)
            favorites.Add(id);
        history.Clear();
        foreach (int id in cleanHistory)
            history.Add(id);
    }

    private static Playlist CopyPlaylist(Playlist playlist)
    {
        List<int> ids = new List<int>();
        foreach (int id in playlist.TrackIds)
            ids.Add(id);
        return new Playlist(playlist.Id, playlist.Name, ids);
    }

    private static int[] CopyIds(ICollection<int> source)
    {
        int[] copy = new int[source.Count];
        int index = 0;
        foreach (int id in source)
        {
            copy[index] = id;
            index++;
        }
        return copy;
    }
}
