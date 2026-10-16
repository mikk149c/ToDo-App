using ToDo_App.UI;

namespace ToDo_App.Tests;

public class LocalizationTests
{
    [Test]
    public void TestPluralityPatterns()
    {
        var englishString = "There [[is {0} note|are {0} notes]] in the column.";
        var danishString = "Der er [[{0} note|{0} noter]] i kolonnen.";
        var naviString = "Tok [['awa 'ul|meul|pxeul|{0}a 'ul]] tsengti.";

        var englishOne = Localizer.HandlePlurals(englishString, 1, "en-US");
        var englishTwo = Localizer.HandlePlurals(englishString, 2, "en-US");
        Assert.That(englishOne, Is.EqualTo("There is 1 note in the column."));
        Assert.That(englishTwo, Is.EqualTo("There are 2 notes in the column."));

        var danishOne = Localizer.HandlePlurals(danishString, 1, "da-DK");
        var danishTwo = Localizer.HandlePlurals(danishString, 2, "da-DK");
        Assert.That(danishOne, Is.EqualTo("Der er 1 note i kolonnen."));
        Assert.That(danishTwo, Is.EqualTo("Der er 2 noter i kolonnen."));

        var naviOne = Localizer.HandlePlurals(naviString, 1, "naq");
        var naviTwo = Localizer.HandlePlurals(naviString, 2, "naq");
        var naviThree = Localizer.HandlePlurals(naviString, 3, "naq");
        var naviFour = Localizer.HandlePlurals(naviString, 4, "naq");
        Assert.That(naviOne, Is.EqualTo("Tok 'awa 'ul tsengti."));
        Assert.That(naviTwo, Is.EqualTo("Tok meul tsengti."));
        Assert.That(naviThree, Is.EqualTo("Tok pxeul tsengti."));
        Assert.That(naviFour, Is.EqualTo("Tok 4a 'ul tsengti."));
    }

    [Test]
    public void TestOctalNumbers()
    {
        var naviString = "Tok [['awa 'ul|meul|pxeul|{0}a 'ul]] tsengti.";
        
        var naviSevenOctal = Localizer.HandlePlurals(naviString, 7, "naq");
        var naviEightOctal = Localizer.HandlePlurals(naviString, 8, "naq");
        var naviNineOctal = Localizer.HandlePlurals(naviString, 9, "naq");
        Assert.That(naviSevenOctal, Is.EqualTo("Tok 7a 'ul tsengti."));
        Assert.That(naviEightOctal, Is.EqualTo("Tok 10a 'ul tsengti."));
        Assert.That(naviNineOctal, Is.EqualTo("Tok 11a 'ul tsengti."));
    }
    [Test]
    public void TestStringWithoutPatternWorks()
    {
        var normalString = "There are notes in the column.";

        var handledString = Localizer.HandlePlurals(normalString, 50, "en-US");
        Assert.That(handledString, Is.EqualTo(normalString));
    }
}