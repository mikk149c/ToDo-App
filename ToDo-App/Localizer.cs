namespace ToDo_App.UI;

public class Localizer
{
    public static readonly Dictionary<string, string> Languages = new()
    {
        {"en-US", "language/english"},
        {"da-DK", "language/danish"},
        {"de", "language/german"},
        {"fo", "language/faroese"},
        {"kln", "language/klingon"},
        {"naq", "language/navi"}
    };

    private static readonly Dictionary<string, List<Func<int,bool>>> PluralityPatterns = new()
    {
        {"en-US", [x => x == 1]},
        {"da-DK", [x => x == 1]},
        {"de",    [x => x == 1]},
        {"fo",    [x => x == 1]},
        {"kln",   [x => x == 1]},
        {"naq",   [x => x == 1, x => x == 2, x => x == 3]}
    };

    public static string HandlePlurals(string localizedString, int number, string language)
    {
        var pattern = PluralityPatterns[language];

        var s = "you have learned [[%1 word|%1 words]] so far.";
        var s2 = "tsole'a [['awa 'uti|meuti|pxeuti|%1a 'uti]] so far.";

        for (int i = 0; i < pattern.Count; i++)
        {
            if (pattern[i].Invoke(number))
            {
                return localizedString; //use pattern
            }
        }
        
        return "Nope";
    }
}