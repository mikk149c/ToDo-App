namespace ToDo_App.Controller;

/// <summary>
/// A board in memory that applies every change straight away. This is how the controller has always worked: saving is
/// still up to whoever listens to BoardChanged.
/// </summary>
public class LocalBoardController : BoardController
{
    public override void SetStatusColumnName(Guid columnId, string newName) => ApplySetStatusColumnName(columnId, newName);

    protected override void CreateColumn(Guid columnId, string name) => ApplyCreateColumn(columnId, name);

    protected override void RemoveColumn(Guid columnId, Guid? moveNotesToColumnId) => ApplyRemoveColumn(columnId, moveNotesToColumnId);

    public override void MoveColumn(Guid columnId, int targetIndex) => ApplyMoveColumn(columnId, targetIndex);

    protected override void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds) =>
        ApplyCreateNote(columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds);

    public override void SetNoteContent(Guid id, string newContent) => ApplySetNoteContent(id, newContent);

    public override void SetNoteTitle(Guid id, string newTitle) => ApplySetNoteTitle(id, newTitle);

    protected override void SetNoteTags(Guid id, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds) =>
        ApplySetNoteTags(id, tagNames, newTagColors, newTagIds);

    public override void RenameTag(Guid tagId, string newName) => ApplyRenameTag(tagId, newName);

    public override void SetTagColor(Guid tagId, string color) => ApplySetTagColor(tagId, color);

    public override void DeleteTag(Guid tagId) => ApplyDeleteTag(tagId);

    protected override void SetNoteFileAttachment(Guid id, AttachedFile file) => ApplySetNoteFileAttachment(id, file);

    public override void RemoveNoteFileAttachment(Guid id) => ApplyRemoveNoteFileAttachment(id);

    public override void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos) =>
        ApplyMoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos);

    public override void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex) =>
        ApplyMoveNoteToColumn(noteId, targetColumnId, targetIndex);

    public override void MoveNoteDown(Guid noteId) => ApplyMoveNoteDown(noteId);

    public override void MoveNoteUp(Guid noteId) => ApplyMoveNoteUp(noteId);

    public override void MoveNoteRight(Guid noteId) => ApplyMoveNoteRight(noteId);

    public override void MoveNoteLeft(Guid noteId) => ApplyMoveNoteLeft(noteId);

    public override void RemoveNote(Guid noteId) => ApplyRemoveNote(noteId);

    protected override void ReplaceBoard(ToDoBoard board) => ApplyReplaceBoard(board);
}
