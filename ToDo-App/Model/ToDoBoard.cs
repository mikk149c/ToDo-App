namespace ToDo_App;

using System.Linq;
using System;

public class ToDoBoard
{
    public List<StatusColumn> StatusColumns { get; set; }
    public StatusColumn TrashBin { get; set; }

    public ToDoBoard()
    {
        StatusColumns = new List<StatusColumn>();
        TrashBin = new StatusColumn("Trash bin");
    }

    public Guid CreateColumn(string title)
    {
        var newColumn = new StatusColumn(title);
        StatusColumns.Add(newColumn);
        return newColumn.Id;
    }

    public void RemoveColumn(Guid id)
    {
        int index = GetColumnIndexFromId(id);
        StatusColumns.RemoveAt(index);
    }

    public void MoveAllNotes(Guid fromColumnId, Guid toColumnId)
    {
        int fromColumnIndex = GetColumnIndexFromId(fromColumnId);
        int toColumnIndex = GetColumnIndexFromId(toColumnId);
        if (fromColumnIndex == toColumnIndex)
        {
            return; //skip since the notes are already in the column
        }

        StatusColumns[toColumnIndex].Notes.AddRange(StatusColumns[fromColumnIndex].Notes);
        StatusColumns[fromColumnIndex].Notes.Clear();
    }

    public void MoveColumn(int fromColumnIndex, int toColumnIndex) //TO DO, discuss if the gui can handle specific column/note indexes
    {
        if (fromColumnIndex < 0 || fromColumnIndex >= StatusColumns.Count || toColumnIndex < 0 || toColumnIndex >= StatusColumns.Count ||fromColumnIndex == toColumnIndex)
        {
            return; //skip if it is going out of bounds or is the same index
        }

        StatusColumn column = StatusColumns[fromColumnIndex];
        StatusColumns.RemoveAt(fromColumnIndex);
        StatusColumns.Insert(toColumnIndex, column);
    }

    public void SetColumnTitle(Guid columnId, string columnTitle)
    {
        int columnIndex = GetColumnIndexFromId(columnId);
        StatusColumns[columnIndex].Title = columnTitle;
    }

    (int columnIndex, int noteIndex) GetNotePositionFromId(Guid id)
    {
        for (int col = 0; col < StatusColumns.Count; col++)
        {
            for (int pos = 0; pos < StatusColumns[col].Notes.Count; pos++)
            {
                if (StatusColumns[col].Notes[pos].Id == id)
                {
                    return (col, pos);
                }
            }
        }

        throw new KeyNotFoundException($"Note id '{id}' does not exist");
    }

    internal int GetColumnIndexFromId(Guid columnId)
    {
        for (int i = 0; i < StatusColumns.Count; i++)
        {
            if (StatusColumns[i].Id == columnId)
                return i;
        }
        throw new KeyNotFoundException($"Column id '{columnId}' does not exist");
    }

    public Guid AddNote(Guid columnId, ToDoNote note)
    {
        int columnIndex = GetColumnIndexFromId(columnId);
        StatusColumns[columnIndex].AddNote(note);
        return note.Id;
    }

    public void RemoveNote(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].RemoveNote(noteIndex);
    }

    public void AddNoteToTrashBin(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        TrashBin.AddNote(StatusColumns[columnIndex].Notes[noteIndex]);
    }

    public void AddColumnNotesToTrashBin(Guid columnId)
    {
        int columnIndex = GetColumnIndexFromId(columnId);
        foreach(ToDoNote note in StatusColumns[columnIndex].Notes)
        {
            TrashBin.AddNote(note);
        }
    }

    public void EmptyTrashBin() { TrashBin.Notes.Clear(); }

    public void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos)
    {
        if (fromColumnIndex < 0 || fromColumnIndex >= StatusColumns.Count ||
        toColumnIndex < 0 || toColumnIndex >= StatusColumns.Count)
        {
            return; //skip since the position doesn't exist
        }
        List<ToDoNote> fromNotes = StatusColumns[fromColumnIndex].Notes;
        List<ToDoNote> toNotes = StatusColumns[toColumnIndex].Notes;

        if (fromPos < 0 || fromPos >= fromNotes.Count)
        {
            return; // skip as there is no note at source.
        }

        int maxPos = fromColumnIndex == toColumnIndex ? toNotes.Count - 1 : toNotes.Count;
        if (toPos < 0 || toPos > maxPos)
        {
            return; //skip since its trying to move beyond the last entry of the column
        }
        if (fromColumnIndex == toColumnIndex && fromPos == toPos)
        {
            return; // skip since the position is the same
        }
        ToDoNote note = fromNotes[fromPos];
        fromNotes.RemoveAt(fromPos);
        toNotes.Insert(toPos, note);

    }

    public void MoveNoteDown(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn, startPos + 1);
    }

    public void MoveNoteUp(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn, startPos - 1);
    }

    public void MoveNoteRight(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn + 1, 0);
    }

    public void MoveNoteLeft(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn - 1, 0);
    }

    public void RemoveNoteFromTrashBin(Guid noteId)
    {
        for (int i = 0; i < TrashBin.Notes.Count; i++)
        {
            if(TrashBin.Notes[i].Id == noteId) 
            {
                TrashBin.RemoveNote(i);
                return;
            }
        }
        
    }

    public void RestoreNoteFromTrashBin(Guid noteId, Guid columnId)
    {
        int columnIndex = GetColumnIndexFromId(columnId);

        for (int i = 0; i < TrashBin.Notes.Count; i++)
        {
            if (TrashBin.Notes[i].Id == noteId)
            {
                ToDoNote note = TrashBin.Notes[i];

                StatusColumns[columnIndex].Notes.Add(note);
                TrashBin.RemoveNote(i);

                return;
            }
        }
    }

    public void SetNoteContent(Guid noteId, string content)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].Notes[noteIndex].Content = content;
    }

    public void SetNoteFile(Guid noteId, AttachedFile? file)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].Notes[noteIndex].File = file;
    }

    public void SetNoteTitle(Guid noteId, string title)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].Notes[noteIndex].Title = title;
    }

    /// <summary>
    /// Replaces a note's tags. Names are validated with <see cref="NoteTag.NormalizeName"/>,
    /// and a name that matches a tag already on the board reuses that tag.
    /// </summary>
    public void SetNoteTags(Guid noteId, IEnumerable<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        var (columnIndex, noteIndex) = GetNotePositionFromId(noteId);
        StatusColumns[columnIndex].Notes[noteIndex].Tags = ResolveTags(tagNames, newTagColors);
    }

    /// <summary>
    /// The tags on the board, worked out from its notes: a tag exists while at least one note has it.
    /// Each tag appears once, sorted by name.
    /// </summary>
    public List<NoteTag> GetTags()
    {
        return AllNotes()
            .SelectMany(note => note.Tags)
            .DistinctBy(tag => tag.Id)
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Turns tag names into tags for a note: validates each name, drops duplicates, and reuses the Id and
    /// spelling and colour of a tag already on the board. A tag that isn't on the board yet takes its colour
    /// from <paramref name="newTagColors"/> (tag name to "#RRGGBB"), or has the default colour.
    /// Nothing is changed if any name or colour is invalid.
    /// </summary>
    /// <exception cref="ArgumentException">A name is empty, only whitespace or too long, or a colour isn't valid.</exception>
    public List<NoteTag> ResolveTags(IEnumerable<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        List<NoteTag> existingTags = GetTags();
        var resolved = new List<NoteTag>();

        foreach (string tagName in tagNames)
        {
            string name = NoteTag.NormalizeName(tagName);
            if (resolved.Exists(tag => NoteTag.NamesMatch(tag.Name, name)))
            {
                continue; // a note can't have the same tag twice
            }

            NoteTag? existing = existingTags.Find(tag => NoteTag.NamesMatch(tag.Name, name));
            if (existing != null)
            {
                resolved.Add(new NoteTag(existing.Name) { Id = existing.Id, Color = existing.Color });
                continue;
            }

            // A brand-new tag starts with the colour chosen for it, if any, otherwise the default
            string? chosenColor = newTagColors?.FirstOrDefault(pair => NoteTag.NamesMatch(pair.Key, name)).Value;
            resolved.Add(new NoteTag(name)
            {
                Color = chosenColor == null ? null : NoteTag.NormalizeColor(chosenColor)
            });
        }

        return resolved;
    }

    /// <summary>
    /// Makes loaded tag data consistent: missing tag lists become empty, a note keeps each tag once,
    /// and when copies of a tag have different names or colours, the first one found wins.
    /// </summary>
    internal void NormalizeTags()
    {
        var namesById = new Dictionary<Guid, string>();
        var colorsById = new Dictionary<Guid, string?>();
        foreach (ToDoNote note in AllNotes())
        {
            note.Tags = (note.Tags ?? new List<NoteTag>()).DistinctBy(tag => tag.Id).ToList();
            foreach (NoteTag tag in note.Tags)
            {
                if (!namesById.TryAdd(tag.Id, tag.Name))
                {
                    tag.Name = namesById[tag.Id];
                }
                if (!colorsById.TryAdd(tag.Id, tag.Color))
                {
                    tag.Color = colorsById[tag.Id];
                }
            }
        }
    }

    /// <summary>
    /// Renames a tag on every note that has it, and returns the name it was given (trimmed).
    /// Changing only the case of the tag's own name is allowed.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No note has the tag.</exception>
    /// <exception cref="ArgumentException">The name isn't valid, or another tag already has it.</exception>
    public string RenameTag(Guid tagId, string newName)
    {
        List<NoteTag> copies = GetTagCopies(tagId);
        string name = NoteTag.NormalizeName(newName);
        if (GetTags().Exists(tag => tag.Id != tagId && NoteTag.NamesMatch(tag.Name, name)))
        {
            throw new ArgumentException($"Another tag is already called \"{name}\".");
        }

        foreach (NoteTag copy in copies)
        {
            copy.Name = name;
        }
        return name;
    }

    /// <summary>Sets the colour of a tag on every note that has it, and returns the colour that was stored.</summary>
    /// <exception cref="KeyNotFoundException">No note has the tag.</exception>
    /// <exception cref="ArgumentException">The colour isn't a "#RRGGBB" hex colour.</exception>
    public string SetTagColor(Guid tagId, string color)
    {
        List<NoteTag> copies = GetTagCopies(tagId);
        string normalized = NoteTag.NormalizeColor(color);

        foreach (NoteTag copy in copies)
        {
            copy.Color = normalized;
        }
        return normalized;
    }

    /// <summary>
    /// Merges one tag into another: every note with <paramref name="sourceTagId"/> gets
    /// <paramref name="targetTagId"/> instead, exactly once, and the source tag no longer exists.
    /// The target keeps its name and colour.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No note has one of the tags.</exception>
    /// <exception cref="ArgumentException">Both are the same tag.</exception>
    public void MergeTags(Guid sourceTagId, Guid targetTagId)
    {
        if (sourceTagId == targetTagId)
        {
            throw new ArgumentException("A tag can't be merged into itself.");
        }

        GetTagCopies(sourceTagId);
        NoteTag target = GetTagCopies(targetTagId)[0];

        foreach (ToDoNote note in AllNotes())
        {
            int index = note.Tags.FindIndex(tag => tag.Id == sourceTagId);
            if (index < 0)
            {
                continue;
            }

            if (HasTag(note, targetTagId))
            {
                note.Tags.RemoveAt(index);
            }
            else
            {
                note.Tags[index] = new NoteTag(target.Name) { Id = target.Id, Color = target.Color };
            }
        }
    }

    /// <summary>Removes a tag from every note that has it. The notes themselves are kept.</summary>
    /// <exception cref="KeyNotFoundException">No note has the tag.</exception>
    public void DeleteTag(Guid tagId)
    {
        GetTagCopies(tagId);
        foreach (ToDoNote note in AllNotes())
        {
            note.Tags.RemoveAll(tag => tag.Id == tagId);
        }
    }

    /// <summary>The Ids of the columns with at least one note that has the tag, in board order.</summary>
    public List<Guid> GetColumnIdsWithTag(Guid tagId)
    {
        return StatusColumns
            .Where(column => column.Notes.Exists(note => HasTag(note, tagId)))
            .Select(column => column.Id)
            .ToList();
    }

    public int CountNotesWithTag(Guid tagId)
    {
        return AllNotes().Count(note => HasTag(note, tagId));
    }

    private IEnumerable<ToDoNote> AllNotes()
    {
        return StatusColumns.SelectMany(column => column.Notes);
    }

    private static bool HasTag(ToDoNote note, Guid tagId)
    {
        return note.Tags.Exists(tag => tag.Id == tagId);
    }

    // Every note stores its own copy of a tag. Throws KeyNotFoundException when no note has it, like the other lookups
    private List<NoteTag> GetTagCopies(Guid tagId)
    {
        List<NoteTag> copies = AllNotes().SelectMany(note => note.Tags).Where(tag => tag.Id == tagId).ToList();
        if (copies.Count == 0)
        {
            throw new KeyNotFoundException($"Tag id '{tagId}' does not exist");
        }
        return copies;
    }

    internal Guid GetColumnIdFromNoteId(Guid noteId)
    {
        // Throws KeyNotFoundException for an unknown note, like the other lookups
        int columnIndex = GetNotePositionFromId(noteId).columnIndex;
        return StatusColumns[columnIndex].Id;
    }

    public void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex)
    {
        var (fromColumnIndex, fromPos) = GetNotePositionFromId(noteId);
        int toColumnIndex = GetColumnIndexFromId(targetColumnId);
        MoveNote(fromColumnIndex, fromPos, toColumnIndex, targetIndex);
    }

    public void MoveColumn (Guid columnId, int targetIndex)
    {
        int fromIndex = GetColumnIndexFromId(columnId);
        MoveColumn(fromIndex, targetIndex);
    }
}