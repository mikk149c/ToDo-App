using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using ToDo_App.Controller;
using ToDo_App.Interfaces;

namespace ToDo_App.Client;

/// <summary>
/// The board on a ToDo-App server. A change is only sent to the server's API: the board here changes when the server
/// sends the change back over SignalR, to every app, this one included. So the UI only ever updates from the server.
/// </summary>
/// <remarks>
/// Create it, subscribe to its events, then call <see cref="ConnectAsync"/>, which loads the server's board through
/// ColumnCreated like loading a file does. Changes from the server are applied on the thread that created the controller
/// (the UI thread in an app), one at a time and in order. Each has a version, and a missed one, or a lost connection,
/// loads the whole board again.
/// </remarks>
public sealed class RemoteBoardController : BoardController, IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    // The server's addresses and message name. ServerBoardController and Program.cs in ToDo-App.Web use the same ones,
    // and RemoteBoardControllerTests runs this class against the real server, so a mismatch fails the tests
    private const string SnapshotPath = "board/snapshot";
    private const string ChangesPath = "commands";
    private const string HubPath = "hub/board";
    private const string ChangedMessage = "Changed";

    /// <summary>The whole board, and the version of the last change in it, as the server sends it.</summary>
    private sealed record Snapshot(long Version, ToDoBoard Board);

    private readonly HttpClient http;
    private readonly HubConnection hub;
    private readonly SynchronizationContext? context = SynchronizationContext.Current;
    private readonly object gate = new object();
    private readonly List<Action> waiting = new List<Action>(); // changes that arrived before the board was loaded
    private bool isLoaded;
    private Task queue = Task.CompletedTask;

    // The version of the last change in the board. Only used on the controller's thread
    private long version;

    /// <summary>
    /// For testing only: how long each change from the server waits before it's applied, to see what a slow server
    /// looks like on a fast network. Zero, the default, applies changes as soon as they arrive.
    /// </summary>
    public TimeSpan IncomingDelay { get; set; } = TimeSpan.Zero;

    /// <param name="serverUrl">The server's address, for example http://localhost:8080/.</param>
    /// <param name="handlerFactory">For tests: makes the HTTP handler to reach the server through.</param>
    public RemoteBoardController(Uri serverUrl, Func<HttpMessageHandler>? handlerFactory = null) : base(new ClientStorageHandler())
    {
        http = handlerFactory == null ? new HttpClient() : new HttpClient(handlerFactory(), disposeHandler: true);
        http.BaseAddress = serverUrl;
        http.Timeout = RequestTimeout;

        hub = new HubConnectionBuilder()
            .WithUrl(new Uri(serverUrl, HubPath), options =>
            {
                if (handlerFactory != null)
                {
                    options.HttpMessageHandlerFactory = _ => handlerFactory();
                    options.Transports = HttpTransportType.LongPolling;
                }
            })
            .WithAutomaticReconnect()
            .Build();
        hub.On<long, string, JsonElement[]>(ChangedMessage, async (changeVersion, change, arguments) =>
        {
            // For testing: hold each change back, as a slow server would. A change that still arrives out of order
            // is a missed version to applyFromServer, which loads the whole board again
            TimeSpan delay = IncomingDelay;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }
            post(() => applyFromServer(changeVersion, change, arguments));
        });
        // Changes sent while the connection was down are lost, so start again from the whole board
        hub.Reconnected += _ =>
        {
            post(reload);
            return Task.CompletedTask;
        };
    }

    /// <summary>Connects to the server and loads its board, raising the same events as loading a board file.</summary>
    /// <exception cref="HttpRequestException">The server can't be reached.</exception>
    public async Task ConnectAsync()
    {
        // Listen first, so no change made while the board loads is missed
        await hub.StartAsync();
        Snapshot snapshot = await http.GetFromJsonAsync<Snapshot>(SnapshotPath)
                            ?? throw new JsonException("The server sent an empty board.");

        base.ReplaceBoard(snapshot.Board);
        version = snapshot.Version;
        lock (gate)
        {
            isLoaded = true;
            foreach (Action change in waiting)
            {
                dispatch(change);
            }
            waiting.Clear();
        }
    }

    // --- Each change goes to the server. It comes back through applyFromServer ---

    public override void UpdateColumn(Guid columnId, string name) =>
        send(nameof(UpdateColumn), columnId, name);

    protected override void CreateColumn(Guid columnId, string name) =>
        send(nameof(CreateColumn), columnId, name);

    public override void DeleteColumn(Guid columnId, Guid? moveNotesToColumnId = null) =>
        send(nameof(DeleteColumn), columnId, moveNotesToColumnId);

    public override void MoveColumn(Guid columnId, int targetIndex) =>
        send(nameof(MoveColumn), columnId, targetIndex);

    protected override void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds) =>
        send(nameof(CreateNote), columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds);

    public override void UpdateNoteTitle(Guid noteId, string title) =>
        send(nameof(UpdateNoteTitle), noteId, title);

    public override void UpdateNoteContent(Guid noteId, string content) =>
        send(nameof(UpdateNoteContent), noteId, content);

    protected override void UpdateNoteTags(Guid noteId, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds) =>
        send(nameof(UpdateNoteTags), noteId, tagNames, newTagColors, newTagIds);

    public override async void UpdateNoteFile(Guid noteId, IFileReference fileReference)
    {
        try
        {
            await using var uploadStream = fileReference.OpenStream();
            var uploadedFileReference = await _storageHandler.SaveAsync(uploadStream, fileReference.Name);
            send(nameof(UpdateNoteFile), noteId, uploadedFileReference);
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    public override void DeleteNoteFile(Guid noteId) =>
        send(nameof(DeleteNoteFile), noteId);

    public override void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex) =>
        send(nameof(MoveNoteToColumn), noteId, targetColumnId, targetIndex);

    public override void TrashNote(Guid noteId) =>
        send(nameof(TrashNote), noteId);

    public override void RestoreNote(Guid noteId, Guid columnId) =>
        send(nameof(RestoreNote), noteId, columnId);

    public override void DeleteNote(Guid noteId) =>
        send(nameof(DeleteNote), noteId);

    public override void EmptyTrash() =>
        send(nameof(EmptyTrash));

    public override void UpdateTagName(Guid tagId, string name) =>
        send(nameof(UpdateTagName), tagId, name);

    public override void UpdateTagColor(Guid tagId, string color) =>
        send(nameof(UpdateTagColor), tagId, color);

    public override void MergeTags(Guid sourceTagId, Guid targetTagId) =>
        send(nameof(MergeTags), sourceTagId, targetTagId);

    public override void DeleteTag(Guid tagId) =>
        send(nameof(DeleteTag), tagId);

    protected override void ReplaceBoard(ToDoBoard board) =>
        send(nameof(ReplaceBoard), board);

    /// <summary>
    /// Sends a change to the server and returns once the server has made it. A change the server refuses throws the
    /// same kind of exception as the local controller does.
    /// </summary>
    /// <exception cref="ArgumentException">The change isn't valid, for example a tag name that's taken.</exception>
    /// <exception cref="KeyNotFoundException">The column, note or tag doesn't exist (any more).</exception>
    private void send(string change, params object?[] arguments)
    {
        // The arguments as a JSON array, in the order of the change method's parameters
        using HttpResponseMessage response = run(() => http.PostAsJsonAsync($"{ChangesPath}/{change}", arguments));
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string message = run(() => response.Content.ReadAsStringAsync());
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ArgumentException(message),
            HttpStatusCode.NotFound => new KeyNotFoundException(message),
            _ => new HttpRequestException($"The server refused the change ({(int)response.StatusCode}): {message}", null, response.StatusCode)
        };
    }

    // --- Changes from the server ---

    private void applyFromServer(long changeVersion, string change, JsonElement[] arguments)
    {
        if (changeVersion <= version)
        {
            return; // already in the board, for example a change made while the board was loading
        }
        if (changeVersion > version + 1)
        {
            reload(); // a change was missed, so the board here can't be trusted any more
            return;
        }

        try
        {
            ApplyChange(change, arguments);
            version = changeVersion;
        }
        catch (Exception)
        {
            // The server made this change, so it should apply here too. If it doesn't, start again from the server's board
            reload();
        }
    }

    /// <summary>
    /// Applies a change the server sent, by name, without sending it anywhere: the server has already made it.
    /// The names are the change methods.
    /// </summary>
    /// <exception cref="KeyNotFoundException">There's no such change, or it names a column, note or tag that doesn't exist.</exception>
    /// <exception cref="ArgumentException">The arguments don't fit the change, or the change isn't valid.</exception>
    internal void ApplyChange(string change, IReadOnlyList<JsonElement> arguments)
    {
        var a = new Arguments(change, arguments);
        switch (change)
        {
            case nameof(UpdateColumn): base.UpdateColumn(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(CreateColumn): base.CreateColumn(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(DeleteColumn): base.DeleteColumn(a.Get<Guid>(0), a.Get<Guid?>(1)); break;
            case nameof(MoveColumn): base.MoveColumn(a.Get<Guid>(0), a.Get<int>(1)); break;
            case nameof(CreateNote):
                base.CreateNote(a.Get<Guid>(0), a.Get<Guid>(1), a.Get<string>(2), a.Get<string>(3), a.Get<AttachedFile?>(4),
                    a.Get<List<string>>(5), a.Get<Dictionary<string, string>?>(6), a.Get<Dictionary<string, Guid>>(7));
                break;
            case nameof(UpdateNoteTitle): base.UpdateNoteTitle(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(UpdateNoteContent): base.UpdateNoteContent(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(UpdateNoteTags):
                base.UpdateNoteTags(a.Get<Guid>(0), a.Get<List<string>>(1), a.Get<Dictionary<string, string>?>(2), a.Get<Dictionary<string, Guid>>(3));
                break;
            case nameof(UpdateNoteFile): base.UpdateNoteFile(a.Get<Guid>(0), a.Get<IFileReference>(1)); break;
            case nameof(DeleteNoteFile): base.DeleteNoteFile(a.Get<Guid>(0)); break;
            case nameof(UpdateTagName): base.UpdateTagName(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(UpdateTagColor): base.UpdateTagColor(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(DeleteTag): base.DeleteTag(a.Get<Guid>(0)); break;
            case nameof(MergeTags): base.MergeTags(a.Get<Guid>(0), a.Get<Guid>(1)); break;
            case nameof(MoveNoteToColumn): base.MoveNoteToColumn(a.Get<Guid>(0), a.Get<Guid>(1), a.Get<int>(2)); break;
            case nameof(TrashNote): base.TrashNote(a.Get<Guid>(0)); break;
            case nameof(DeleteNote): base.DeleteNote(a.Get<Guid>(0)); break;
            case nameof(RestoreNote): base.RestoreNote(a.Get<Guid>(0), a.Get<Guid>(1)); break;
            case nameof(EmptyTrash): base.EmptyTrash(); break;
            case nameof(ReplaceBoard): base.ReplaceBoard(a.Get<ToDoBoard>(0)); break;
            default: throw new KeyNotFoundException($"There's no change called '{change}'.");
        }
    }

    private void reload()
    {
        try
        {
            Snapshot? snapshot = run(() => http.GetFromJsonAsync<Snapshot>(SnapshotPath));
            if (snapshot != null)
            {
                base.ReplaceBoard(snapshot.Board);
                version = snapshot.Version;
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            // The server is gone for now. Reconnecting loads the board again
        }
    }

    // Runs an action on the controller's thread, once the board is loaded
    private void post(Action action)
    {
        lock (gate)
        {
            if (!isLoaded)
            {
                waiting.Add(action);
                return;
            }
            dispatch(action);
        }
    }

    // On the controller's thread, or in order on the thread pool when there is no such thread (tests)
    private void dispatch(Action action)
    {
        void safely()
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                // A failure in one change must never stop the next. applyFromServer reloads when it matters
            }
        }

        if (context != null)
        {
            context.Post(_ => safely(), null);
            return;
        }
        queue = queue.ContinueWith(_ => safely(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    // The board interface is synchronous. The network isn't, so wait on the thread pool, which never deadlocks the UI thread
    private static void run(Func<Task> work) => Task.Run(work).GetAwaiter().GetResult();

    private static T run<T>(Func<Task<T>> work) => Task.Run(work).GetAwaiter().GetResult();

    public void Dispose()
    {
        try
        {
            Task.Run(() => hub.DisposeAsync().AsTask()).Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Closing a connection that's already broken
        }
        http.Dispose();
    }

    // Reads the arguments of a change from the server: a JSON array, in the order of the change method's parameters
    private sealed class Arguments(string change, IReadOnlyList<JsonElement> arguments)
    {
        /// <exception cref="ArgumentException">The argument is missing, or isn't a <typeparamref name="T"/>.</exception>
        public T Get<T>(int index)
        {
            if (index >= arguments.Count)
            {
                throw new ArgumentException($"{change} needs more than {arguments.Count} arguments.");
            }

            try
            {
                return arguments[index].Deserialize<T>(JsonSerializerOptions.Web)!;
            }
            catch (JsonException e)
            {
                throw new ArgumentException($"Argument {index + 1} of {change} isn't valid: {e.Message}", e);
            }
        }
    }
}
