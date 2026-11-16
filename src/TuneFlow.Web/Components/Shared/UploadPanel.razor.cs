using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TuneFlow.Core;

namespace TuneFlow.Web.Components.Shared;

public partial class UploadPanel
{
    [Parameter] public EventCallback<Track> OnUploaded { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private readonly string inputId = "upload-" + Guid.NewGuid().ToString("N");
    private string title = "";
    private string artist = "";
    private string? error;
    private bool busy;

    private async Task Import()
    {
        if (busy)
            return;
        error = null;

        Result<(string Title, string Artist)> validation = LibraryState.CheckUploadText(title, artist);
        if (validation.IsFailure)
        {
            error = validation.Error;
            return;
        }
        if (Library.UploadCount >= 50)
        {
            error = "You can import up to 50 songs.";
            return;
        }

        busy = true;
        try
        {
            string key = Guid.NewGuid().ToString("N");
            double duration = await JS.InvokeAsync<double>("tune.importTrack", inputId, key);
            Track track = new Track(Library.NextUploadId, validation.Value.Title,
                validation.Value.Artist, "Uploads", "Personal", 0, "#b6a0ff",
                (int)Math.Ceiling(duration), key);
            try
            {
                await OnUploaded.InvokeAsync(track);
            }
            catch
            {
                await JS.InvokeVoidAsync("tune.removeImport", key);
                throw;
            }
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
        }
        catch (JSException exception)
        {
            error = exception.Message.Split('\n')[0].Replace("Error: ", "");
        }
        finally
        {
            busy = false;
        }
    }
}
