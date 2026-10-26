namespace ToDo_App.Interfaces;

/// <summary>
/// What to change on a note, for <see cref="ITodoBoard.UpdateNote"/>. A field left null stays as it is, so two people
/// changing different parts of the same note don't overwrite each other.
/// </summary>
public sealed record NoteChanges
{
    public string? Title { get; init; }

    public string? Content { get; init; }

    /// <summary>The note's new tags, by name. They replace all its tags.</summary>
    public IReadOnlyList<string>? TagNames { get; init; }

    /// <summary>The colour for each tag name that isn't a tag on the board yet (tag name to "#RRGGBB").</summary>
    public IReadOnlyDictionary<string, string>? NewTagColors { get; init; }

    /// <summary>A file to attach, replacing the one the note has.</summary>
    public AttachedFile? File { get; init; }

    /// <summary>Removes the note's file. Can't be used together with <see cref="File"/>.</summary>
    public bool RemoveFile { get; init; }

    /// <summary>Whether anything is set, so a dialog closed without changes sends nothing.</summary>
    public bool HasChanges() =>
        Title != null || Content != null || TagNames != null || File != null || RemoveFile;

    /// <summary>
    /// What an edit dialog changed: only the fields that differ from the note as it was when the dialog opened are set.
    /// So a field someone else changed in the meantime isn't put back to the value the dialog started with.
    /// </summary>
    public static NoteChanges Since(OpenedNote opened, string title, string content, IReadOnlyList<string> tagNames,
        IReadOnlyDictionary<string, string>? newTagColors)
    {
        bool tagsChanged = !tagNames.SequenceEqual(opened.TagNames);
        return new NoteChanges
        {
            Title = title == opened.Title ? null : title,
            Content = content == opened.Content ? null : content,
            TagNames = tagsChanged ? tagNames : null,
            NewTagColors = tagsChanged ? newTagColors : null
        };
    }
}

/// <summary>A note's text and tags as they were when an edit dialog opened, for <see cref="NoteChanges.Since"/>.</summary>
public sealed record OpenedNote(string Title, string Content, IReadOnlyList<string> TagNames)
{
    // The tag names are copied, as renaming a tag changes the note's tags in place
    public static OpenedNote Of(ToDoNote note) =>
        new OpenedNote(note.Title, note.Content, note.Tags.Select(tag => tag.Name).ToList());
}
