namespace TuneFlow.Core;

// A record represents one song. The browser snapshot uses these property names.
public sealed record Track(
    int Id,
    string Title,
    string Artist,
    string Genre,
    string Mood,
    int Bpm,
    string Color,
    int Duration = 32,
    string? LocalAudioKey = null)
{
    public string Audio
    {
        get
        {
            if (LocalAudioKey == null)
                return $"/audio/{Id}.wav";

            return $"local:{LocalAudioKey}";
        }
    }
}
