namespace TuneFlow.Core;

public sealed record Playlist(Guid Id, string Name, List<int> TrackIds);
