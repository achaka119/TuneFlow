using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TuneFlow.Core;
using TuneFlow.Web.Components.Pages;
using TuneFlow.Web.Components.Shared;

// Server-side component integration checks. These do not simulate a browser.
ServiceCollection collection = new ServiceCollection();
collection.AddLogging();
collection.AddSingleton<LibraryState>();
collection.AddSingleton<IJSRuntime, NoBrowserJavaScript>();
using ServiceProvider services = collection.BuildServiceProvider();
await using HtmlRenderer renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
int passed = 0;

await renderer.Dispatcher.InvokeAsync(async () =>
{
    var root = await renderer.RenderComponentAsync<Home>(ParameterView.Empty);
    string html = root.ToHtmlString();
    Check(html.Contains("Find your") && html.Contains("Soft Horizons") && html.Contains("Music player"),
        "Home markup and code-behind render together");

    services.GetRequiredService<LibraryState>().ToggleFavorite(5);
    var rankedRoot = await renderer.RenderComponentAsync<Home>(ParameterView.Empty);
    string rankedHtml = rankedRoot.ToHtmlString();
    Check(rankedHtml.IndexOf("Quiet Orbit", StringComparison.Ordinal) <
        rankedHtml.IndexOf("Soft Horizons", StringComparison.Ordinal),
        "Handwritten ranking reaches the rendered recommendation cards");

    var upload = await renderer.RenderComponentAsync<UploadPanel>(ParameterView.Empty);
    string uploadHtml = upload.ToHtmlString();
    Check(uploadHtml.Contains("Song title") && uploadHtml.Contains("Artist") &&
        uploadHtml.Contains("type=\"file\"") && uploadHtml.Contains("Import song"),
        "UploadPanel code-behind retains all import controls");
});
Console.WriteLine($"{passed} component render checks passed");

void Check(bool condition, string name)
{
    if (!condition)
        throw new Exception(name);
    Console.WriteLine($"PASS {name}");
    passed++;
}

sealed class NoBrowserJavaScript : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return ValueTask.FromResult(default(TValue)!);
    }
}
