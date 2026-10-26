using System.Text.Json;
using ToDo_App.Controller;
using ToDo_App.Interfaces;

namespace ToDo_App.Web.Board;

/// <summary>
/// The server's controller: a <see cref="LocalBoardController"/>, as the server's board is the truth, that makes one
/// change at a time and sends every change, numbered, to the remote apps. Changes from the web pages and from the API
/// both come through here, so they can never interleave.
/// </summary>
public sealed class ServerBoardController : LocalBoardController
{
    private readonly object gate = new object();
    private readonly IChangeBroadcaster? broadcaster;

    /// <param name="broadcaster">Where changes are sent, or null when no remote apps can connect (tests).</param>
    public ServerBoardController(IChangeBroadcaster? broadcaster = null)
    {
        this.broadcaster = broadcaster;
    }

    /// <summary>The number of changes made since the server started.</summary>
    public long Version { get; private set; }

    /// <summary>
    /// The board and the version it's at, as JSON (<c>{ "version": …, "board": … }</c>), read together so a remote app
    /// knows which changes to apply next.
    /// </summary>
    public string SnapshotJson()
    {
        lock (gate)
        {
            return JsonSerializer.Serialize(new { version = Version, board = Board }, JsonSerializerOptions.Web);
        }
    }

    /// <summary>
    /// Makes a change that came in through the API, by name. It goes through the same method as a change from a web
    /// page, so it's applied and sent on to every remote app the same way.
    /// </summary>
    /// <exception cref="KeyNotFoundException">There's no such change, or it names a column, note or tag that doesn't exist.</exception>
    /// <exception cref="ArgumentException">The arguments don't fit the change, or the change isn't valid.</exception>
    public void Execute(string change, IReadOnlyList<JsonElement> arguments)
    {
        var a = new Arguments(change, arguments);
        switch (change)
        {
            case nameof(UpdateColumn): UpdateColumn(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(CreateColumn): CreateColumn(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(DeleteColumn): DeleteColumn(a.Get<Guid>(0), a.Get<Guid?>(1)); break;
            case nameof(MoveColumn): MoveColumn(a.Get<Guid>(0), a.Get<int>(1)); break;
            case nameof(CreateNote):
                CreateNote(a.Get<Guid>(0), a.Get<Guid>(1), a.Get<string>(2), a.Get<string>(3), a.Get<AttachedFile?>(4),
                    a.Get<List<string>>(5), a.Get<Dictionary<string, string>?>(6), a.Get<Dictionary<string, Guid>>(7));
                break;
            case nameof(UpdateNote): UpdateNote(a.Get<Guid>(0), a.Get<NoteChanges>(1), a.Get<Dictionary<string, Guid>>(2)); break;
            case nameof(UpdateTag): UpdateTag(a.Get<Guid>(0), a.Get<string?>(1), a.Get<string?>(2)); break;
            case nameof(DeleteTag): DeleteTag(a.Get<Guid>(0)); break;
            case nameof(MergeTags): MergeTags(a.Get<Guid>(0), a.Get<Guid>(1)); break;
            case nameof(MoveNoteToColumn): MoveNoteToColumn(a.Get<Guid>(0), a.Get<Guid>(1), a.Get<int>(2)); break;
            case nameof(TrashNote): TrashNote(a.Get<Guid>(0)); break;
            case nameof(DeleteNote): DeleteNote(a.Get<Guid>(0)); break;
            case nameof(RestoreNote): RestoreNote(a.Get<Guid>(0), a.Get<Guid>(1)); break;
            case nameof(EmptyTrash): EmptyTrash(); break;
            case nameof(ReplaceBoard): ReplaceBoard(a.Get<ToDoBoard>(0)); break;
            default: throw new KeyNotFoundException($"There's no change called '{change}'.");
        }
    }

    // --- Every change, from a web page or the API: made as on any local board, then sent on ---

    public override void UpdateColumn(Guid columnId, string name) =>
        applyAndBroadcast(() => base.UpdateColumn(columnId, name), nameof(UpdateColumn), columnId, name);

    protected override void CreateColumn(Guid columnId, string name) =>
        applyAndBroadcast(() => base.CreateColumn(columnId, name), nameof(CreateColumn), columnId, name);

    public override void DeleteColumn(Guid columnId, Guid? moveNotesToColumnId = null) =>
        applyAndBroadcast(() => base.DeleteColumn(columnId, moveNotesToColumnId), nameof(DeleteColumn), columnId, moveNotesToColumnId);

    public override void MoveColumn(Guid columnId, int targetIndex) =>
        applyAndBroadcast(() => base.MoveColumn(columnId, targetIndex), nameof(MoveColumn), columnId, targetIndex);

    protected override void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds) =>
        applyAndBroadcast(() => base.CreateNote(columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds),
            nameof(CreateNote), columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds);

    protected override void UpdateNote(Guid noteId, NoteChanges changes, IReadOnlyDictionary<string, Guid> newTagIds) =>
        applyAndBroadcast(() => base.UpdateNote(noteId, changes, newTagIds), nameof(UpdateNote), noteId, changes, newTagIds);

    public override void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex) =>
        applyAndBroadcast(() => base.MoveNoteToColumn(noteId, targetColumnId, targetIndex),
            nameof(MoveNoteToColumn), noteId, targetColumnId, targetIndex);

    public override void TrashNote(Guid noteId) =>
        applyAndBroadcast(() => base.TrashNote(noteId), nameof(TrashNote), noteId);

    public override void RestoreNote(Guid noteId, Guid columnId) =>
        applyAndBroadcast(() => base.RestoreNote(noteId, columnId), nameof(RestoreNote), noteId, columnId);

    public override void DeleteNote(Guid noteId) =>
        applyAndBroadcast(() => base.DeleteNote(noteId), nameof(DeleteNote), noteId);

    public override void EmptyTrash() =>
        applyAndBroadcast(() => base.EmptyTrash(), nameof(EmptyTrash));

    public override void UpdateTag(Guid tagId, string? name = null, string? color = null) =>
        applyAndBroadcast(() => base.UpdateTag(tagId, name, color), nameof(UpdateTag), tagId, name, color);

    public override void MergeTags(Guid sourceTagId, Guid targetTagId) =>
        applyAndBroadcast(() => base.MergeTags(sourceTagId, targetTagId), nameof(MergeTags), sourceTagId, targetTagId);

    public override void DeleteTag(Guid tagId) =>
        applyAndBroadcast(() => base.DeleteTag(tagId), nameof(DeleteTag), tagId);

    protected override void ReplaceBoard(ToDoBoard board) =>
        applyAndBroadcast(() => base.ReplaceBoard(board), nameof(ReplaceBoard), board);

    // Makes a change and, if it was valid, sends it on. The arguments are written as JSON before the lock is let go:
    // a board passed to ReplaceBoard is the live board afterwards, and keeps changing
    private void applyAndBroadcast(Action apply, string change, params object?[] arguments)
    {
        lock (gate)
        {
            apply();
            Version++;
            broadcaster?.Enqueue(new BoardChange(Version, change, JsonSerializer.SerializeToElement(arguments, JsonSerializerOptions.Web)));
        }
    }

    // Reads the arguments of a change from the API: a JSON array, in the order of the change method's parameters
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
