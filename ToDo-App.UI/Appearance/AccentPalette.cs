using Windows.UI;

namespace ToDo_App.UI.Appearance
{
    public sealed record AccentColorOption(string Name, string Hex)
    {
        public Color Color => ParseHex(Hex);

        public static Color ParseHex(string hex)
        {
            return Color.FromArgb(
                0xFF,
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16));
        }
    }

    /// <summary>
    /// The selectable accent colours, laid out as a 3x3 grid. Every colour keeps white text
    /// readable (WCAG AA), because buttons draw white content on top of the accent.
    /// </summary>
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
}
