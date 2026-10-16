namespace ToDo_App.Web.Appearance;

public sealed record AccentColorOption(string Name, string Hex);

/// <summary>
/// The selectable accent colours, laid out as a 3x3 grid. Every colour keeps white text readable (WCAG AA),
/// because buttons draw white content on top of the accent. Tag colours are picked from the same palette,
/// and a tag with no colour gets <see cref="Default"/>.
/// </summary>
/// <remarks>The same colours as <c>ToDo-App.UI/Appearance/AccentPalette.cs</c>, so a board looks the same in both apps.</remarks>
public static class AccentPalette
{
    public const int Columns = 3;

    public static AccentColorOption Default { get; } = new("Purple", "#512BD4");

    public static IReadOnlyList<AccentColorOption> All { get; } = new[]
    {
        Default,
        new AccentColorOption("Blue", "#0F6CBD"),
        new AccentColorOption("Teal", "#00796B"),
        new AccentColorOption("Green", "#2E7D32"),
        new AccentColorOption("Orange", "#C2410C"),
        new AccentColorOption("Red", "#C62828"),
        new AccentColorOption("Pink", "#AD1457"),
        new AccentColorOption("Brown", "#6D4C41"),
        new AccentColorOption("Slate", "#455A64"),
    };

    public static AccentColorOption Find(string? hex)
    {
        return All.FirstOrDefault(option => string.Equals(option.Hex, hex, StringComparison.OrdinalIgnoreCase))
               ?? Default;
    }
}
