namespace ToDo_App.UI;

public class Localizer
{
    public static readonly Dictionary<string, string> Languages = new()
    {
        {"en-US", "English"},
        {"da-DK", "dansk"},
        {"de", "deutsch"},
        {"eo", "Esperanto"},
        {"fo", "føroyskt"},
        {"kln", "tlhIngan Hol"},
        {"naq", "lì'fya leNa'vi"}
    };

    private static readonly Dictionary<string, List<Func<int,bool>>> PluralityPatterns = new()
    {
        {"en-US", [x => x == 1]},
        {"da-DK", [x => x == 1]},
        {"de",    [x => x == 1]},
        {"eo",    [x => x == 1]},
        {"fo",    [x => x == 1]},
        {"kln",   [x => x == 1]},
        {"naq",   [x => x == 1, x => x == 2, x => x == 3]}
    };

    /// <summary>
    /// Transforms a localized string with different plural forms into a single string based on a specific number.
    /// </summary>
    /// <param name="localizedString">A string that can include the syntax
    /// "[[first form|second form|third form]]" and "{x}", where x can be any integer</param>
    /// <param name="number">The plural form that matches this number will be used</param>
    /// <param name="language">The plurality pattern of this language will be used ("en-US", "da-DK", "de", etc.)</param>
    /// <param name="variableIndex">Optional parameter to specify which {x} to replace with the number argument</param>
    /// <returns></returns>
    public static string HandlePlurals(string localizedString, int number, string language, int variableIndex = 0)
    {
        var numberString = language == "naq" ? Convert.ToString(number, 8) : $"{number}";
        var stringWithNumber = localizedString.Replace($"{{{variableIndex}}}", numberString);
        
        var startPos = stringWithNumber.IndexOf("[[", StringComparison.Ordinal);
        var endPos = stringWithNumber.IndexOf("]]", StringComparison.Ordinal);
        
        if (startPos == -1 || endPos == -1)
        {
            return stringWithNumber;
        }
        
        var beforeString = stringWithNumber.Substring(0,startPos);
        var matchedString = stringWithNumber.Substring(startPos + 2, endPos - startPos - 2);
        var afterString = stringWithNumber.Substring(endPos + 2, stringWithNumber.Length - endPos - 2);
        
        var forms = matchedString.Split('|');
        
        // find a plurality pattern that matches the input number
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