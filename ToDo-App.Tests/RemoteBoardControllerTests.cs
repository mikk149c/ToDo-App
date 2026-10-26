namespace ToDo_App.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ToDo_App.Client;
using ToDo_App.Controller;
using ToDo_App.Interfaces;
using ToDo_App.Web.Board;

// The whole remote loop, with the web app hosted in memory: RemoteBoardControllers send changes through
// the API, and only see them when the server sends them back over SignalR
public class RemoteBoardControllerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private string dataDirectory;
    private WebApplicationFactory<Program> server;
    private readonly List<RemoteBoardController> clients = new();

    [SetUp]
    public void Setup()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), $"todo-remote-tests-{Guid.NewGuid()}");
        server = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Board:DataDirectory", dataDirectory));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (RemoteBoardController client in clients)
        {
            client.Dispose(); // closes its connection
        }
        clients.Clear();
        server.Dispose();
        if (Directory.Exists(dataDirectory))
        {
            Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private RemoteBoardController connect(out EventRecorder recorder)
    {
        var controller = new RemoteBoardController(server.Server.BaseAddress, () => server.Server.CreateHandler());
        clients.Add(controller);
        recorder = new EventRecorder(controller);
        controller.ConnectAsync().GetAwaiter().GetResult();
        return controller;
    }

    private BoardHost host => server.Services.GetRequiredService<BoardHost>();

    private static void waitUntil(Func<bool> condition, string what)
    {
        DateTime end = DateTime.UtcNow + Timeout;
        while (!holds(condition))
        {
            if (DateTime.UtcNow > end)
            {
                Assert.Fail($"Timed out waiting for {what}");
            }
            Thread.Sleep(20);
        }
    }

    // Tests have no UI thread, so a client applies the server's changes on a background thread. Reading its board while
    // it's in the middle of one can fail, so that counts as "not yet"
    private static bool holds(Func<bool> condition)
    {
        try
        {
            return condition();
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    [Test]
    public void AClientStartsFromTheServersBoard()
    {
        RemoteBoardController client = connect(out EventRecorder recorder);

        Assert.That(client.GetStatusColumnIds(), Is.Not.Empty);
        Assert.That(client.GetStatusColumnIds(), Is.EqualTo(host.Board.GetStatusColumnIds()));
        Assert.That(recorder.Names, Does.Contain(nameof(ITodoBoard.ColumnCreated)));
    }

    [Test]
    public void AChangeReachesEveryClientThroughTheServer()
    {
        RemoteBoardController first = connect(out EventRecorder firstRecorder);
        RemoteBoardController second = connect(out EventRecorder secondRecorder);
        firstRecorder.Clear();
        secondRecorder.Clear();

        Guid columnId = first.CreateColumn("Review");

        // The sender's own board also changes only when the server sends the change back
        waitUntil(() => first.GetStatusColumnIds().Contains(columnId) && second.GetStatusColumnIds().Contains(columnId), "both clients");
        Assert.That(host.Board.GetStatusColumnName(columnId), Is.EqualTo("Review"));
        Assert.That(firstRecorder.Names, Does.Contain(nameof(ITodoBoard.ColumnCreated)));
        Assert.That(secondRecorder.Names, Does.Contain(nameof(ITodoBoard.ColumnCreated)));
    }

    [Test]
    public void ANoteTitleChangeReachesTheOtherClient()
    {
        RemoteBoardController first = connect(out _);
        RemoteBoardController second = connect(out EventRecorder recorder);
        Guid columnId = first.GetStatusColumnIds()[0];
        Guid noteId = first.CreateNote(columnId, "Before", "");
        waitUntil(() => second.GetNotesInColumn(columnId).Exists(note => note.Id == noteId), "the note");
        recorder.Clear();

        first.UpdateNoteTitle(noteId, "After");

        waitUntil(() => second.GetNotesInColumn(columnId).Single(note => note.Id == noteId).Title == "After", "the new title");
        Assert.That(recorder.Names, Is.EqualTo(new[] { nameof(ITodoBoard.ColumnContentChanged), nameof(ITodoBoard.BoardChanged) }));
    }

    // The developer delay in the Windows app: changes from the server wait, and still end up applied in order
    [Test]
    public void IncomingDelayHoldsChangesFromTheServerBack()
    {
        RemoteBoardController client = connect(out _);
        client.IncomingDelay = TimeSpan.FromMilliseconds(400);
        Guid columnId = client.GetStatusColumnIds()[0];
        DateTime start = DateTime.UtcNow;

        Guid noteId = host.Board.CreateNote(columnId, "First", "");
        host.Board.UpdateNoteTitle(noteId, "Second");

        Thread.Sleep(150);
        Assert.That(holds(() => client.GetNotesInColumn(columnId).Exists(note => note.Id == noteId)), Is.False, "held back");
        waitUntil(() => client.GetNotesInColumn(columnId).SingleOrDefault(note => note.Id == noteId)?.Title == "Second", "both changes");
        Assert.That(DateTime.UtcNow - start, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(400)));
    }

    [Test]
    public void NewNotesAndTagsGetTheSameIdsEverywhere()
    {
        RemoteBoardController first = connect(out _);
        RemoteBoardController second = connect(out _);
        Guid columnId = first.GetStatusColumnIds()[0];

        Guid noteId = first.CreateNote(columnId, "Remote note", "", tagNames: new[] { "Work" });

        waitUntil(() => second.GetNotesInColumn(columnId).Exists(note => note.Id == noteId), "the note on the second client");
        waitUntil(() => first.GetTags().Count == 1, "the tag on the first client");
        Guid serverTagId = host.Board.GetTags().Single().Id;
        Assert.That(second.GetTags().Single().Id, Is.EqualTo(serverTagId));
        Assert.That(first.GetTags().Single().Id, Is.EqualTo(serverTagId));
    }

    [Test]
    public void TagsSetOnOneClientReachTheOther()
    {
        RemoteBoardController first = connect(out _);
        RemoteBoardController second = connect(out _);
        Guid columnId = first.GetStatusColumnIds()[0];
        Guid noteId = first.CreateNote(columnId, "Note", "");
        waitUntil(() => second.GetNotesInColumn(columnId).Exists(note => note.Id == noteId), "the note");

        first.UpdateNoteTags(noteId, new[] { "Urgent" }, new Dictionary<string, string> { ["Urgent"] = "#FF0000" });

        waitUntil(() => second.GetTags().Count == 1, "the tag on the second client");
        Assert.That(second.GetTags().Single().Color, Is.EqualTo("#FF0000"));
        Assert.That(second.GetTags().Single().Id, Is.EqualTo(host.Board.GetTags().Single().Id));
    }

    [Test]
    public void AChangeInTheWebAppReachesRemoteClients()
    {
        RemoteBoardController client = connect(out _);

        Guid columnId = host.Board.CreateColumn("From the browser");

        waitUntil(() => client.GetStatusColumnIds().Contains(columnId), "the change from the web app");
    }

    [Test]
    public void ImportingOnAClientReplacesTheServersBoard()
    {
        RemoteBoardController client = connect(out _);
        string file = Path.Combine(Path.GetTempPath(), $"todo-import-{Guid.NewGuid()}.json");
        var imported = new LocalBoardController();
        Guid importedColumnId = imported.CreateColumn("Imported");
        imported.SaveBoardToJson(file);
        try
        {
            client.LoadBoardFromJson(file);
        }
        finally
        {
            File.Delete(file);
        }

        Assert.That(host.Board.GetStatusColumnIds(), Is.EqualTo(new[] { importedColumnId }));
        waitUntil(() => client.GetStatusColumnIds().SequenceEqual(new[] { importedColumnId }), "the imported board on the client");
    }

    [Test]
    public void ARefusedChangeThrowsTheSameExceptionAsLocally()
    {
        RemoteBoardController client = connect(out _);
        Guid columnId = client.GetStatusColumnIds()[0];
        client.CreateNote(columnId, "Note", "", tagNames: new[] { "Work", "Home" });
        waitUntil(() => client.GetTags().Count == 2, "the tags");
        Guid homeId = client.GetTags().Single(tag => tag.Name == "Home").Id;

        Assert.Throws<ArgumentException>(() => client.UpdateTagName(homeId, "work"));
        Assert.Throws<KeyNotFoundException>(() => client.TrashNote(Guid.NewGuid()));
    }

    [Test]
    public void ChangesArriveInOrder()
    {
        RemoteBoardController first = connect(out _);
        RemoteBoardController second = connect(out _);

        for (int i = 0; i < 10; i++)
        {
            first.CreateColumn($"Column {i}");
        }

        waitUntil(() => second.GetStatusColumnIds().Count == host.Board.GetStatusColumnIds().Count, "all columns on the second client");
        waitUntil(() => first.GetStatusColumnIds().Count == host.Board.GetStatusColumnIds().Count, "all columns on the first client");
        Assert.That(second.GetStatusColumnIds(), Is.EqualTo(host.Board.GetStatusColumnIds()));
        Assert.That(first.GetStatusColumnIds(), Is.EqualTo(host.Board.GetStatusColumnIds()));
    }

    [Test]
    public void AnUnreachableServerFailsToConnect()
    {
        using var controller = new RemoteBoardController(new Uri("http://127.0.0.1:9/"));

        Assert.ThrowsAsync<HttpRequestException>(controller.ConnectAsync);
    }

    // Every change can arrive over the network, so each one needs a case in both switches: the server's
    // (ServerBoardController.Execute, from the API) and the remote app's (RemoteBoardController.ApplyChange, from SignalR)
    [Test]
    public void EveryChangeCanBeReceivedByName()
    {
        MethodInfo[] methods = typeof(BoardController)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsVirtual && !method.IsFinal)
            .ToArray();
        using var remote = new RemoteBoardController(new Uri("http://127.0.0.1:9/"));

        Assert.That(methods.Select(method => method.Name).Distinct().Count(), Is.GreaterThanOrEqualTo(15));
        foreach (IGrouping<string, MethodInfo> change in methods.GroupBy(method => method.Name))
        {
            if (change.Any(method => method.GetParameters().Length == 0))
            {
                // Nothing to refuse: a change with a case just runs, one without isn't found
                Assert.DoesNotThrow(() => host.Board.Execute(change.Key, []), $"server: {change.Key}");
                Assert.DoesNotThrow(() => remote.ApplyChange(change.Key, []), $"remote: {change.Key}");
                continue;
            }

            // No arguments: a change with a case refuses them, one without isn't found
            Assert.Throws<ArgumentException>(() => host.Board.Execute(change.Key, []), $"server: {change.Key}");
            Assert.Throws<ArgumentException>(() => remote.ApplyChange(change.Key, []), $"remote: {change.Key}");
        }
    }

    [Test]
    public void TheApiRefusesUnknownAndInvalidChanges()
    {
        HttpClient http = server.CreateClient();
        Guid columnId = host.Board.GetStatusColumnIds()[0];

        HttpResponseMessage unknown = http.PostAsJsonAsync("commands/Explode", Array.Empty<object>()).GetAwaiter().GetResult();
        HttpResponseMessage wrongArguments = http.PostAsJsonAsync("commands/UpdateColumn", new object[] { columnId }).GetAwaiter().GetResult();
        HttpResponseMessage wrongType = http.PostAsJsonAsync("commands/UpdateColumn", new object[] { columnId, new[] { 1, 2 } }).GetAwaiter().GetResult();
        Assert.That(wrongType.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        HttpResponseMessage valid = http.PostAsJsonAsync("commands/UpdateColumn", new object[] { columnId, "Renamed" }).GetAwaiter().GetResult();

        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(wrongArguments.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(valid.IsSuccessStatusCode, Is.True);
        Assert.That(host.Board.GetStatusColumnName(columnId), Is.EqualTo("Renamed"));
    }
}
