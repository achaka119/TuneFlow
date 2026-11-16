using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text.Json;
using TuneFlow.Core;

namespace TuneFlow.Web.Components.Pages;

// Markup is in Home.razor; this partial class contains its C# event handlers.
public partial class Home
{
    private string view = "Discover";
    private string mood = "Focus";
    private string query = "";
    private string genre = "All";
    private string playlistName = "";
    private string? message;
    private string? dialog;
    private string? dialogError;
    private Guid? activePlaylist;
    private Guid? renaming;
    private bool confirmDelete;
    private bool queueOpen;
    private bool playing;
    private bool shuffle;
    private bool repeat;
    private bool disposed;
    private bool uploadOpen;
    private int adding;
    private Track? current;
    private List<Track> queue = Catalog.CopyTracks(Catalog.Tracks);
    private double position;
    private double volume = .7;
    private DotNetObjectReference<Home>? reference;

    private static string NavIcon(string name)
    {
        switch (name)
        {
            case "Discover": return "◈";
            case "Search": return "⌕";
            case "Favorites": return "♡";
            case "Playlists": return "▤";
            default: return "◷";
        }
    }

    private void Navigate(string name)
    {
        view = name;
        confirmDelete = false;
    }

    private void SelectPlaylist(Guid id)
    {
        activePlaylist = id;
        confirmDelete = false;
    }

    private List<Track> Visible()
    {
        if (view == "Favorites")
            return Library.FavoriteTracks();
        if (view == "History")
            return Library.RecentTracks();
        return Catalog.Search(query, genre, Library.Tracks);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        reference = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("tune.init", reference);
        try
        {
            string? json = await JS.InvokeAsync<string?>("tune.load");
            if (json != null)
            {
                LibrarySnapshot? snapshot = JsonSerializer.Deserialize<LibrarySnapshot>(json);
                if (snapshot == null)
                    throw new ArgumentException("Invalid data");
                Library.Restore(snapshot);
            }
        }
        catch (Exception error) when (error is JsonException or ArgumentException or JSException)
        {
            message = "Saved library could not be loaded. You can continue with a fresh library.";
        }
        StateHasChanged();
    }

    private async Task Persist()
    {
        try
        {
            string json = JsonSerializer.Serialize(Library.Snapshot());
            await JS.InvokeVoidAsync("tune.save", json);
        }
        catch (JSException)
        {
            message = "Browser storage is unavailable. Changes will last for this session.";
        }
    }

    private async Task Uploaded(Track track)
    {
        Library.RegisterUpload(track);
        await Persist();
        query = "";
        genre = "Uploads";
        uploadOpen = false;
        message = "Song imported. You can play it, like it or add it to a playlist.";
    }

    private async Task Like(int id)
    {
        Library.ToggleFavorite(id);
        await Persist();
    }

    private void SetMood(string selectedMood)
    {
        mood = selectedMood;
    }

    private async Task PlayMix()
    {
        queue = Catalog.Mix(mood, Library.Favorites, Library.Tracks);
        if (queue.Count > 0)
            await Play(queue[0]);
    }

    private async Task PlayPlaylist(Playlist playlist)
    {
        queue = Library.PlaylistTracks(playlist);
        if (queue.Count > 0)
            await Play(queue[0]);
    }

    private async Task Play(Track track)
    {
        if (Catalog.FindTrackIndex(queue, track.Id) < 0)
            queue = Catalog.CopyTracks(Library.Tracks);

        current = track;
        position = 0;
        Library.Record(track.Id);
        await Persist();
        try
        {
            await JS.InvokeVoidAsync("tune.play", track.Audio, volume);
        }
        catch (JSException error)
        {
            playing = false;
            if (error.Message.Contains("no longer in browser storage"))
                message = "This song is no longer in browser storage. Upload it again.";
            else
                message = "Playback could not start. Press Play to retry.";
        }
    }

    private async Task Toggle()
    {
        if (current == null)
        {
            await PlayMix();
            return;
        }
        try
        {
            await JS.InvokeVoidAsync("tune.toggle");
        }
        catch (JSException)
        {
            message = "Playback unavailable. Please try again.";
        }
    }

    private async Task Next()
    {
        if (current == null || queue.Count == 0)
            return;

        int index = Catalog.FindTrackIndex(queue, current.Id);
        int nextIndex;
        if (shuffle && queue.Count > 1)
            nextIndex = (index + Random.Shared.Next(1, queue.Count)) % queue.Count;
        else
            nextIndex = (index + 1) % queue.Count;

        await Play(queue[nextIndex]);
    }

    private async Task Previous()
    {
        if (current == null || queue.Count == 0)
            return;
        int index = Catalog.FindTrackIndex(queue, current.Id);
        if (index < 0)
            index = 0;
        int previousIndex = (index + queue.Count - 1) % queue.Count;
        await Play(queue[previousIndex]);
    }

    [JSInvokable]
    public async Task Ended()
    {
        if (disposed)
            return;
        if (repeat && current != null)
            await Play(current);
        else
            await Next();
        StateHasChanged();
    }

    [JSInvokable]
    public void Progress(double seconds)
    {
        if (disposed)
            return;
        position = seconds;
        StateHasChanged();
    }

    [JSInvokable]
    public void Playback(bool value)
    {
        if (disposed)
            return;
        playing = value;
        StateHasChanged();
    }

    [JSInvokable]
    public void AudioError()
    {
        if (disposed)
            return;
        playing = false;
        message = "Audio could not load. Choose another track or retry.";
        StateHasChanged();
    }

    private async Task Seek(ChangeEventArgs args)
    {
        if (double.TryParse(args.Value?.ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double seconds))
            await JS.InvokeVoidAsync("tune.seek", seconds);
    }

    private async Task Volume(ChangeEventArgs args)
    {
        if (double.TryParse(args.Value?.ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double value))
        {
            volume = value;
            await JS.InvokeVoidAsync("tune.volume", value);
        }
    }

    private static string Time(double seconds)
    {
        int wholeSeconds = (int)seconds;
        return $"{wholeSeconds / 60}:{wholeSeconds % 60:00}";
    }

    private void OpenCreate()
    {
        dialog = "create";
        playlistName = "";
        dialogError = null;
    }

    private void OpenRename(Playlist playlist)
    {
        dialog = "rename";
        renaming = playlist.Id;
        playlistName = playlist.Name;
        dialogError = null;
    }

    private void OpenAdd(int id)
    {
        adding = id;
        dialog = "add";
        dialogError = null;
    }

    private void CloseDialog()
    {
        dialog = null;
        dialogError = null;
    }

    private void DialogKey(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
            CloseDialog();
    }

    private async Task SavePlaylist()
    {
        Result<Playlist> result;
        if (dialog == "rename" && renaming is Guid id)
            result = Library.TryRename(id, playlistName);
        else
            result = Library.TryCreate(playlistName);

        if (result.IsFailure)
        {
            dialogError = result.Error;
            return;
        }

        activePlaylist = result.Value.Id;
        await Persist();
        dialog = null;
        view = "Playlists";
        message = "Playlist saved.";
    }

    private async Task AddTo(Guid id)
    {
        Library.Add(id, adding);
        await Persist();
        dialog = null;
        message = "Track added to playlist.";
    }

    private async Task Remove(Guid id, int track)
    {
        Library.Remove(id, track);
        await Persist();
    }

    private async Task DeletePlaylist(Guid id)
    {
        Library.Delete(id);
        activePlaylist = null;
        confirmDelete = false;
        await Persist();
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        try
        {
            await JS.InvokeVoidAsync("tune.dispose");
        }
        catch (JSDisconnectedException)
        {
            // The browser has already disconnected; there is nothing to dispose there.
        }
        reference?.Dispose();
    }
}
