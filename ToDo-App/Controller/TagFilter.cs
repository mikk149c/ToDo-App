namespace ToDo_App.Controller;

/// <summary>How a todo is matched against several selected tags.</summary>
public enum TagMatchMode
{
    /// <summary>The todo has at least one of the selected tags.</summary>
    Any,
    /// <summary>The todo has every one of the selected tags.</summary>
    All
}

/// <summary>
/// Which todos the board shows, chosen by tag (#67, #69). It only decides what is shown: it never changes
/// the board, so nothing is lost and <c>board.json</c> keeps every todo. Tags are followed by <see cref="NoteTag.Id"/>,
/// so renaming a selected tag keeps the filter on. The filter isn't saved, so it's off after a restart.
/// </summary>
public class TagFilter
{
    private readonly List<Guid> _tagIds = new();

    /// <summary>The Ids of the selected tags, in the order they were selected.</summary>
    public IReadOnlyList<Guid> TagIds => _tagIds;

    public TagMatchMode Mode { get; set; } = TagMatchMode.Any;

    /// <summary>True while at least one tag is selected. Without a filter every todo is shown.</summary>
    public bool IsActive => _tagIds.Count > 0;

    public void Select(Guid tagId)
    {
        if (!_tagIds.Contains(tagId))
        {
            _tagIds.Add(tagId);
        }
    }

    public void Deselect(Guid tagId)
    {
        _tagIds.Remove(tagId);
    }

    /// <summary>Turns the filter off, so all todos are shown again.</summary>
    public void Clear()
    {
        _tagIds.Clear();
    }

    public bool Matches(ToDoNote note)
    {
        if (!IsActive)
        {
            return true;
        }

        return Mode == TagMatchMode.All
            ? _tagIds.TrueForAll(tagId => note.Tags.Exists(tag => tag.Id == tagId))
            : _tagIds.Exists(tagId => note.Tags.Exists(tag => tag.Id == tagId));
    }

    /// <summary>The notes that are shown, in their original order.</summary>
    public List<ToDoNote> Apply(IEnumerable<ToDoNote> notes)
    {
        return notes.Where(Matches).ToList();
    }

    /// <summary>
    /// Deselects tags that no longer exist on the board, so the filter turns itself off when the last
    /// selected tag is gone. Returns true if anything was deselected.
    /// </summary>
    public bool Prune(IEnumerable<NoteTag> existingTags)
    {
        var existingIds = existingTags.Select(tag => tag.Id).ToHashSet();
        return _tagIds.RemoveAll(tagId => !existingIds.Contains(tagId)) > 0;
    }

    /// <summary>
    /// Translates a position among the shown notes of a column into a position among all of its notes, so a
    /// drop on a filtered column leaves the hidden notes where they are. A position after the last shown note
    /// is just after that note.
    /// </summary>
    /// <param name="allNotes">Every note in the column, in order.</param>
    /// <param name="shownIndex">A position among the notes that <see cref="Apply"/> returns.</param>
    public int ToColumnIndex(IReadOnlyList<ToDoNote> allNotes, int shownIndex)
    {
        if (!IsActive)
        {
            return shownIndex;
        }

        List<ToDoNote> shown = Apply(allNotes);
        if (shown.Count == 0)
        {
            return allNotes.Count;
        }

        bool afterLastShown = shownIndex >= shown.Count;
        ToDoNote anchor = shown[afterLastShown ? shown.Count - 1 : shownIndex];
        int anchorIndex = allNotes.ToList().FindIndex(note => note.Id == anchor.Id);
        return afterLastShown ? anchorIndex + 1 : anchorIndex;
    }
}
