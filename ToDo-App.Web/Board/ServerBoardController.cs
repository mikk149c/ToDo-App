using System.Text.Json;
using ToDo_App.Controller;

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

    /// <summary>The board and the version it's at, read together so a remote app knows which changes to apply next.</summary>
    public string SnapshotJson()
    {
        lock (gate)
        {
            return BoardProtocol.Serialize(new BoardSnapshot(Version, Board));
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
        var a = new ChangeArguments(change, arguments);
        switch (change)
        {
            case nameof(SetStatusColumnName): SetStatusColumnName(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(CreateColumn): CreateColumn(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(RemoveColumn): RemoveColumn(a.Get<Guid>(0), a.Get<Guid?>(1)); break;
            case nameof(MoveColumn): MoveColumn(a.Get<Guid>(0), a.Get<int>(1)); break;
            case nameof(CreateNote):
                CreateNote(a.Get<Guid>(0), a.Get<Guid>(1), a.Get<string>(2), a.Get<string>(3), a.Get<AttachedFile?>(4),
                    a.Get<List<string>>(5), a.Get<Dictionary<string, string>?>(6), a.Get<Dictionary<string, Guid>>(7));
                break;
            case nameof(SetNoteContent): SetNoteContent(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(SetNoteTitle): SetNoteTitle(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(SetNoteTags):
                SetNoteTags(a.Get<Guid>(0), a.Get<List<string>>(1), a.Get<Dictionary<string, string>?>(2), a.Get<Dictionary<string, Guid>>(3));
                break;
            case nameof(RenameTag): RenameTag(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(SetTagColor): SetTagColor(a.Get<Guid>(0), a.Get<string>(1)); break;
            case nameof(DeleteTag): DeleteTag(a.Get<Guid>(0)); break;
            case nameof(SetNoteFileAttachment): SetNoteFileAttachment(a.Get<Guid>(0), a.Get<AttachedFile>(1)); break;
            case nameof(RemoveNoteFileAttachment): RemoveNoteFileAttachment(a.Get<Guid>(0)); break;
            case nameof(MoveNote): MoveNote(a.Get<int>(0), a.Get<int>(1), a.Get<int>(2), a.Get<int>(3)); break;
            case nameof(MoveNoteToColumn): MoveNoteToColumn(a.Get<Guid>(0), a.Get<Guid>(1), a.Get<int>(2)); break;
            case nameof(MoveNoteDown): MoveNoteDown(a.Get<Guid>(0)); break;
            case nameof(MoveNoteUp): MoveNoteUp(a.Get<Guid>(0)); break;
            case nameof(MoveNoteRight): MoveNoteRight(a.Get<Guid>(0)); break;
            case nameof(MoveNoteLeft): MoveNoteLeft(a.Get<Guid>(0)); break;
            case nameof(RemoveNote): RemoveNote(a.Get<Guid>(0)); break;
            case nameof(ReplaceBoard): ReplaceBoard(a.Get<ToDoBoard>(0)); break;
            default: throw new KeyNotFoundException($"There's no change called '{change}'.");
        }
    }

    // --- Every change, from a web page or the API: made as on any local board, then sent on ---

    public override void SetStatusColumnName(Guid columnId, string newName) =>
        applyAndBroadcast(() => base.SetStatusColumnName(columnId, newName), nameof(SetStatusColumnName), columnId, newName);

    protected override void CreateColumn(Guid columnId, string name) =>
        applyAndBroadcast(() => base.CreateColumn(columnId, name), nameof(CreateColumn), columnId, name);

    protected override void RemoveColumn(Guid columnId, Guid? moveNotesToColumnId) =>
        applyAndBroadcast(() => base.RemoveColumn(columnId, moveNotesToColumnId), nameof(RemoveColumn), columnId, moveNotesToColumnId);

    public override void MoveColumn(Guid columnId, int targetIndex) =>
        applyAndBroadcast(() => base.MoveColumn(columnId, targetIndex), nameof(MoveColumn), columnId, targetIndex);

    protected override void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds) =>
        applyAndBroadcast(() => base.CreateNote(columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds),
            nameof(CreateNote), columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds);

    public override void SetNoteContent(Guid id, string newContent) =>
        applyAndBroadcast(() => base.SetNoteContent(id, newContent), nameof(SetNoteContent), id, newContent);

    public override void SetNoteTitle(Guid id, string newTitle) =>
        applyAndBroadcast(() => base.SetNoteTitle(id, newTitle), nameof(SetNoteTitle), id, newTitle);

    protected override void SetNoteTags(Guid id, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds) =>
        applyAndBroadcast(() => base.SetNoteTags(id, tagNames, newTagColors, newTagIds),
            nameof(SetNoteTags), id, tagNames, newTagColors, newTagIds);

    public override void RenameTag(Guid tagId, string newName) =>
        applyAndBroadcast(() => base.RenameTag(tagId, newName), nameof(RenameTag), tagId, newName);

    public override void SetTagColor(Guid tagId, string color) =>
        applyAndBroadcast(() => base.SetTagColor(tagId, color), nameof(SetTagColor), tagId, color);

    public override void DeleteTag(Guid tagId) =>
        applyAndBroadcast(() => base.DeleteTag(tagId), nameof(DeleteTag), tagId);

    protected override void SetNoteFileAttachment(Guid id, AttachedFile file) =>
        applyAndBroadcast(() => base.SetNoteFileAttachment(id, file), nameof(SetNoteFileAttachment), id, file);

    public override void RemoveNoteFileAttachment(Guid id) =>
        applyAndBroadcast(() => base.RemoveNoteFileAttachment(id), nameof(RemoveNoteFileAttachment), id);

    public override void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos) =>
        applyAndBroadcast(() => base.MoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos),
            nameof(MoveNote), fromColumnIndex, fromPos, toColumnIndex, toPos);

    public override void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex) =>
        applyAndBroadcast(() => base.MoveNoteToColumn(noteId, targetColumnId, targetIndex),
            nameof(MoveNoteToColumn), noteId, targetColumnId, targetIndex);

    public override void MoveNoteDown(Guid noteId) =>
        applyAndBroadcast(() => base.MoveNoteDown(noteId), nameof(MoveNoteDown), noteId);

    public override void MoveNoteUp(Guid noteId) =>
        applyAndBroadcast(() => base.MoveNoteUp(noteId), nameof(MoveNoteUp), noteId);

    public override void MoveNoteRight(Guid noteId) =>
        applyAndBroadcast(() => base.MoveNoteRight(noteId), nameof(MoveNoteRight), noteId);

    public override void MoveNoteLeft(Guid noteId) =>
        applyAndBroadcast(() => base.MoveNoteLeft(noteId), nameof(MoveNoteLeft), noteId);

    public override void RemoveNote(Guid noteId) =>
        applyAndBroadcast(() => base.RemoveNote(noteId), nameof(RemoveNote), noteId);

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
            broadcaster?.Enqueue(new BoardChange(Version, change, ChangeArguments.ToJson(arguments)));
        }
    }
}
