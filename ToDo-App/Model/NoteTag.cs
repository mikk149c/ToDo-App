using System.Text.RegularExpressions;

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

    /// <summary>
    /// The tag's colour as "#RRGGBB", or null for the default colour. Every todo with the tag stores the
    /// same colour, so boards saved before tags had colours simply show the default.
    /// </summary>
    public string? Color { get; set; }

    public NoteTag(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>Trims a tag name and checks it's 1 to <see cref="MaxNameLength"/> characters long.</summary>
    /// <exception cref="ArgumentException">
    /// The name is empty, only whitespace, or too long. The message is written to be shown to the user.
    /// </exception>
    public static string NormalizeName(string? name)
    {
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A tag needs a name.");
        }
        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"A tag name can be at most {MaxNameLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Checks a colour is written as "#RRGGBB" and returns it in upper case.</summary>
    /// <exception cref="ArgumentException">The colour isn't a "#RRGGBB" hex colour.</exception>
    public static string NormalizeColor(string? color)
    {
        string trimmed = color?.Trim() ?? "";
        if (!Regex.IsMatch(trimmed, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new ArgumentException("A tag colour must be written as #RRGGBB.");
        }

        return trimmed.ToUpperInvariant();
    }

    /// <summary>Tag names are matched without regard to case, so "work" and "Work" are the same tag.</summary>
    public static bool NamesMatch(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
