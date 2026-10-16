using System.Globalization;
using System.Resources;

namespace ToDo_App.Web.Localization;

/// <summary>
/// The translated texts for one browser tab, read from the same .resx files as the Windows app
/// (linked from <c>ToDo-App.UI/Properties</c>). Changing <see cref="CurrentCulture"/> raises
/// <see cref="Changed"/>, so the page can redraw in the new language.
/// </summary>
public sealed class UiStrings
{
    public const string DefaultLanguage = "en-US";

    // Language code to the key of its name, as in the Windows app
    public static readonly IReadOnlyDictionary<string, string> Languages = new Dictionary<string, string>
    {
        {"en-US", "language/english"},
        {"da-DK", "language/danish"},
        {"de", "language/german"},
        {"fo", "language/faroese"},
        {"kln", "language/klingon"},
        {"naq", "language/navi"}
    };

    private static readonly ResourceManager Resources =
        new ResourceManager("ToDo_App.Web.Properties.Resources", typeof(UiStrings).Assembly);

    public CultureInfo CurrentCulture { get; private set; } = new CultureInfo(DefaultLanguage);

    public string CurrentLanguage { get; private set; } = DefaultLanguage;

    public event EventHandler? Changed;

    /// <summary>The text for a key. A missing translation shows the key instead of failing.</summary>
    public string this[string key] => Resources.GetString(key, CurrentCulture) ?? key;

    /// <summary>The text for a key with <see cref="string.Format(string, object[])"/> placeholders filled in.</summary>
    public string Format(string key, params object[] args)
    {
        return string.Format(this[key], args);
    }

    /// <summary>Switches language. An unknown language code falls back to the default language.</summary>
    public void SetLanguage(string? language)
    {
        string chosen = language != null && Languages.ContainsKey(language) ? language : DefaultLanguage;
        if (chosen == CurrentLanguage)
        {
            return;
        }

        CurrentLanguage = chosen;
        CurrentCulture = new CultureInfo(chosen);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
