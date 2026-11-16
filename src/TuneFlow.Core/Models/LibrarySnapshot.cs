namespace TuneFlow.Core;

// Optional Uploads keeps snapshots from the first app version readable.
public sealed record LibrarySnapshot(
    int Version,
    int[] Favorites,
    Playlist[] Playlists,
    int[] History,
    Track[]? Uploads = null);
