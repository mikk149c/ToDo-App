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
}
