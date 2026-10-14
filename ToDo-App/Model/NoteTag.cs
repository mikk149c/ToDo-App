namespace ToDo_App;

/// <summary>
/// A tag on a todo. The <see cref="Id"/> is the tag's identity, so a tag can later be renamed
/// without becoming a different tag. Every todo with the tag stores its own copy with the same Id.
/// </summary>
public class NoteTag
{
    public const int MaxNameLength = 12;

    public Guid Id { get; set; }
    public string Name { get; set; }

    public NoteTag(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>Trims a tag name and checks it's 1 to <see cref="MaxNameLength"/> characters long.</summary>
    /// <exception cref="ArgumentException">The name is empty, only whitespace, or too long.</exception>
    public static string NormalizeName(string? name)
    {
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A tag needs a name.", nameof(name));
        }
        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"A tag name can be at most {MaxNameLength} characters.", nameof(name));
        }

        return trimmed;
    }

    /// <summary>Tag names are matched without regard to case, so "work" and "Work" are the same tag.</summary>
    public static bool NamesMatch(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
