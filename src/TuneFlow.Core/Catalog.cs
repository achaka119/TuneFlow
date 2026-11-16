namespace TuneFlow.Core;

public static class Catalog
{
    private static readonly Track[] demoTracks =
    {
        new Track(1, "Midnight Drive", "TuneFlow Studio", "Electronic", "Energy", 112, "#b6a0ff"),
        new Track(2, "Soft Horizons", "TuneFlow Studio", "Ambient", "Focus", 72, "#f2b48b"),
        new Track(3, "After the Rain", "TuneFlow Studio", "Lo-fi", "Relax", 84, "#83bba7"),
        new Track(4, "Neon Pulse", "TuneFlow Studio", "Electronic", "Energy", 128, "#f099b5"),
        new Track(5, "Quiet Orbit", "TuneFlow Studio", "Ambient", "Focus", 64, "#92b4df"),
        new Track(6, "Sunday Coffee", "TuneFlow Studio", "Lo-fi", "Relax", 90, "#d5c987")
    };

    public static IReadOnlyList<Track> Tracks { get; } = Array.AsReadOnly(demoTracks);

    // Filtering is written explicitly: inspect each song and add matching songs.
    public static List<Track> Search(
        string? query, string genre = "All", IEnumerable<Track>? source = null)
    {
        string text = query == null ? "" : query.Trim();
        List<Track> matches = new List<Track>();

        foreach (Track track in source ?? Tracks)
        {
            if (genre != "All" && track.Genre != genre)
                continue;

            string keywords = $"{track.Title} {track.Artist} {track.Genre} {track.Mood}";
            if (text.Length == 0 || keywords.Contains(text, StringComparison.OrdinalIgnoreCase))
                matches.Add(track);
        }

        return matches;
    }

    // Pure function: the score depends only on its arguments.
    public static int Score(Track track, string mood, IEnumerable<int> favorites)
    {
        int score = 0;
        if (track.Mood == mood)
            score += 10;

        foreach (int id in favorites)
        {
            if (id == track.Id)
            {
                score += 2;
                break;
            }
        }

        return score;
    }

    public static List<Track> Mix(
        string mood, IEnumerable<int> favorites, IEnumerable<Track>? source = null)
    {
        List<Track> ordered = CopyTracks(source ?? Tracks);
        List<int> liked = new List<int>();
        foreach (int id in favorites)
            liked.Add(id);

        // Insertion sort: move lower-ranked songs to the right and insert the song.
        // The catalog has at most 56 tracks, so this simple O(n²) algorithm is adequate.
        for (int i = 1; i < ordered.Count; i++)
        {
            Track song = ordered[i];
            int songScore = Score(song, mood, liked);
            int j = i - 1;

            while (j >= 0)
            {
                int previousScore = Score(ordered[j], mood, liked);
                bool songComesFirst = songScore > previousScore ||
                    (songScore == previousScore && song.Id < ordered[j].Id);
                if (!songComesFirst)
                    break;

                ordered[j + 1] = ordered[j];
                j--;
            }

            ordered[j + 1] = song;
        }

        return ordered;
    }

    public static List<Track> CopyTracks(IEnumerable<Track> source)
    {
        List<Track> copy = new List<Track>();
        foreach (Track track in source)
            copy.Add(track);
        return copy;
    }

    public static List<Track> FirstTracks(IEnumerable<Track> source, int count)
    {
        List<Track> result = new List<Track>();
        if (count <= 0)
            return result;

        foreach (Track track in source)
        {
            result.Add(track);
            if (result.Count == count)
                break;
        }
        return result;
    }

    public static int FindTrackIndex(IReadOnlyList<Track> tracks, int id)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].Id == id)
                return i;
        }
        return -1;
    }
}
