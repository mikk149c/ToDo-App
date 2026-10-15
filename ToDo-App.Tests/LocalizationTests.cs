using ToDo_App.UI;

namespace ToDo_App.Tests;

public class LocalizationTests
{
    [Test]
    public void TestPlurality()
    {
        var englishString = "There [[is %1 note|are %1 notes]] in the column.";
        var danishString = "Der er [[%1 note|%1 noter]] i kolonnen.";
        var naviString = "Tok [['awa 'ul|meul|pxeul|%1a 'ul]] tsengti.";

        var englishOne = Localizer.HandlePlurals(englishString, 1, "en-US");
        var englishTwo = Localizer.HandlePlurals(englishString, 1, "en-US");
        Assert.That(englishOne, Is.EqualTo("There is 1 note in the column."));
        Assert.That(englishTwo, Is.EqualTo("There are 2 notes in the column."));
        
        var danishOne = Localizer.HandlePlurals(englishString, 1, "da-Dk");
        var danishTwo = Localizer.HandlePlurals(englishString, 1, "da-Dk");
        Assert.That(danishOne, Is.EqualTo("Der er 1 note i kolonnen."));
        Assert.That(danishTwo, Is.EqualTo("Der er 2 noter i kolonnen."));
        
        var naviOne = Localizer.HandlePlurals(englishString, 1, "naq");
        var naviTwo = Localizer.HandlePlurals(englishString, 2, "naq");
        var naviThree = Localizer.HandlePlurals(englishString, 3, "naq");
        var naviFour = Localizer.HandlePlurals(englishString, 4, "naq");
        Assert.That(naviOne, Is.EqualTo("Tok 'awa 'ul tsengti."));
        Assert.That(naviTwo, Is.EqualTo("Tok meul tsengti."));
        Assert.That(naviThree, Is.EqualTo("Tok pxeul tsengti."));
        Assert.That(naviFour, Is.EqualTo("Tok 4a 'ul tsengti."));
        
        var naviSevenOctal = Localizer.HandlePlurals(englishString, 7, "naq");
        var naviEightOctal = Localizer.HandlePlurals(englishString, 8, "naq");
        var naviNineOctal = Localizer.HandlePlurals(englishString, 9, "naq");
        Assert.That(naviSevenOctal, Is.EqualTo("Tok 7a 'ul tsengti."));
        Assert.That(naviEightOctal, Is.EqualTo("Tok 10a 'ul tsengti."));
        Assert.That(naviNineOctal, Is.EqualTo("Tok 11a 'ul tsengti."));
    }
}