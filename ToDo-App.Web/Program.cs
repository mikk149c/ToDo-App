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
builder.Services.AddSingleton<AttachmentStore>();

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

// Opens a todo's attachment. Only files uploaded through the browser are served (see AttachmentStore.IsStored)
app.MapGet("/attachments/{noteId:guid}", (Guid noteId, BoardHost host, AttachmentStore attachments, HttpContext context) =>
{
    AttachedFile? file = host.FindNote(noteId)?.File;
    if (file == null || !attachments.IsStored(file.FullPath))
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
