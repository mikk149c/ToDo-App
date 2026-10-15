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

    public static string HandlePlurals(string localizedString, int number, string language, int variableIndex = 0)
    {
        var stringWithNumber = localizedString.Replace($"{{{variableIndex}}}", $"{number}");

        var startPos = stringWithNumber.IndexOf("[[");
        var endPos = stringWithNumber.IndexOf("]]");
        
        var beforeString = stringWithNumber.Substring(0,startPos);
        var matchedString = stringWithNumber.Substring(startPos + 2, endPos - startPos - 2);
        var afterString = stringWithNumber.Substring(endPos + 2, stringWithNumber.Length - endPos - 2);
        
        var forms = matchedString.Split('|');

        var pattern = PluralityPatterns[language];
        for (int i = 0; i < pattern.Count; i++)
        {
            if (pattern[i].Invoke(number))
            {
                return $"{beforeString}{forms[i]}{afterString}";
            }
        }
        
        // if there is no match, use the final form
        return $"{beforeString}{forms[^1]}{afterString}";
    }
}