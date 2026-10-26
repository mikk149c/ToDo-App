using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using ToDo_App;
using ToDo_App.Web.Appearance;
using ToDo_App.Web.Board;
using ToDo_App.Web.Components;
using ToDo_App.Web.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One board for the whole server, kept in "Board:DataDirectory" (App_Data next to the app by default)
string dataDirectory = Path.GetFullPath(
    builder.Configuration["Board:DataDirectory"] ?? "App_Data",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new BoardStorageOptions(dataDirectory));
builder.Services.AddSingleton<BoardHost>();

// Remote desktop apps: changes come in through the API, and every change goes out to every app through the hub
builder.Services.AddSignalR();
builder.Services.AddSingleton<SignalRBroadcaster>();
builder.Services.AddSingleton<IChangeBroadcaster>(services => services.GetRequiredService<SignalRBroadcaster>());
builder.Services.AddHostedService(services => services.GetRequiredService<SignalRBroadcaster>());

// Per browser tab: language and appearance are UI preferences, remembered in the browser
builder.Services.AddScoped<UiStrings>();
builder.Services.AddScoped<BrowserPreferences>();
builder.Services.AddScoped<AppearanceManager>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- The board API for remote apps. RemoteBoardController (ToDo-App.Client) uses the same addresses ---

// The whole board and its version, to start from
app.MapGet("/board/snapshot", (BoardHost host) => Results.Text(host.Board.SnapshotJson(), "application/json"));

// A change: a controller change method by name, with its arguments. It's made, saved and sent to every app through
// the hub, which is how the sender sees it too
app.MapPost("/commands/{change}", (string change, JsonElement[] arguments, BoardHost host) =>
{
    try
    {
        host.Board.Execute(change, arguments);
        return Results.NoContent();
    }
    catch (ArgumentException e)
    {
        return Results.Text(e.Message, statusCode: StatusCodes.Status400BadRequest);
    }
    catch (KeyNotFoundException e)
    {
        return Results.Text(e.Message, statusCode: StatusCodes.Status404NotFound);
    }
});

app.MapHub<BoardHub>("/hub/board");

// Export: the browser downloads the board as a .json file
app.MapGet("/board/export", (BoardHost host) =>
{
    string tempPath = Path.Combine(Path.GetTempPath(), $"todo-export-{Guid.NewGuid()}.json");
    try
    {
        host.Board.SaveBoardToJson(tempPath);
        return Results.File(File.ReadAllBytes(tempPath), "application/json", "board.json");
    }
    finally
    {
        File.Delete(tempPath);
    }
});

// Opens a todo's attachment
app.MapGet("/attachments/{noteId:guid}", (Guid noteId, BoardHost host, HttpContext context) =>
{
    AttachedFile? file = host.FindNote(noteId)?.File;
    if (file == null || !host.Board.IsFileStored(file.FullPath))
    {
        return Results.NotFound();
    }

    if (!new FileExtensionContentTypeProvider().TryGetContentType(file.Name, out string? contentType))
    {
        contentType = "application/octet-stream";
    }
    // Inline, so the browser shows what it can (images, PDFs) and downloads the rest
    var disposition = new ContentDispositionHeaderValue("inline");
    disposition.SetHttpFileName(file.Name);
    context.Response.Headers.ContentDisposition = disposition.ToString();
    // An uploaded .html or .svg must not run scripts as this site
    context.Response.Headers.ContentSecurityPolicy = "sandbox";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    return Results.File(file.FullPath, contentType);
});

app.Run();

// Public, so tests can host the whole server in memory (WebApplicationFactory<Program>)
public partial class Program;
