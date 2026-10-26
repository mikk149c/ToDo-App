using Microsoft.AspNetCore.Components.Forms;

namespace ToDo_App.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using ToDo_App.Web.Appearance;
using ToDo_App.Web.Board;
using ToDo_App.Web.Localization;

// The web app's services that don't need a browser: loading and saving the board, attachments,
// translations and the accent colour (#91)
public class WebAppTests
{
    private string dataDirectory;

    [SetUp]
    public void Setup()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), $"todo-web-tests-{Guid.NewGuid()}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(dataDirectory))
        {
            Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private BoardHost createHost()
    {
        return new BoardHost(new BoardStorageOptions(dataDirectory), NullLogger<BoardHost>.Instance);
    }

    private string boardPath => Path.Combine(dataDirectory, "board.json");

    [Test]
    public void NewServerStartsWithTheDefaultBoard()
    {
        BoardHost host = createHost();

        List<string> names = host.Board.GetStatusColumnIds().Select(host.Board.GetStatusColumnName).ToList();
        Assert.That(names, Is.EqualTo(new[] { "Planned", "In Progress", "Completed" }));
        Assert.That(host.LoadError, Is.Null);
    }

    [Test]
    public void ChangesAreSavedAndLoadedAgain()
    {
        BoardHost host = createHost();
        Guid columnId = host.Board.CreateColumn("Review");
        host.Board.CreateNote(columnId, "Saved", "on the server", tagNames: new[] { "Web" });

        BoardHost restarted = createHost();

        Assert.That(restarted.Board.GetStatusColumnIds(), Does.Contain(columnId));
        ToDoNote note = restarted.Board.GetNotesInColumn(columnId).Single();
        Assert.That(note.Title, Is.EqualTo("Saved"));
        Assert.That(note.Tags.Single().Name, Is.EqualTo("Web"));
    }

    [Test]
    public void AnUnreadableBoardIsNotOverwrittenJustByStarting()
    {
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(boardPath, "not json");

        BoardHost host = createHost();

        // The default board is shown, but the file stays as it was until the user changes something
        Assert.That(host.Board.GetStatusColumnIds(), Has.Count.EqualTo(3));
        Assert.That(File.ReadAllText(boardPath), Is.EqualTo("not json"));
    }

    [Test]
    public void SaveCompletedIsRaisedForEveryChange()
    {
        BoardHost host = createHost();
        int saves = 0;
        host.SaveCompleted += (sender, e) => saves++;

        host.Board.CreateColumn("One");
        host.Board.CreateColumn("Two");

        Assert.That(saves, Is.EqualTo(2));
        Assert.That(host.SaveError, Is.Null);
    }

    [Test]
    public void FindNoteLooksInEveryColumn()
    {
        BoardHost host = createHost();
        Guid lastColumn = host.Board.GetStatusColumnIds().Last();
        Guid noteId = host.Board.CreateNote(lastColumn, "Find me", "");

        Assert.That(host.FindNote(noteId)?.Title, Is.EqualTo("Find me"));
        Assert.That(host.FindNote(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task UploadedAttachmentIsStoredUnderItsOwnName()
    {
        var store = new ServerStorageHandler(new BoardStorageOptions(dataDirectory));

        var fileReference = await store.SaveAsync(new MemoryStream("hello"u8.ToArray()), "notes.txt");
        var path = fileReference.FullPath;

        Assert.That(Path.GetFileName(path), Is.EqualTo("notes.txt"));
        Assert.That(File.ReadAllText(path), Is.EqualTo("hello"));
        Assert.That(store.IsStored(path), Is.True);
    }

    [Test]
    public async Task AttachmentNameCannotEscapeTheAttachmentFolder()
    {
        var store = new ServerStorageHandler(new BoardStorageOptions(dataDirectory));

        var fileReference = await store.SaveAsync(new MemoryStream("x"u8.ToArray()), @"..\..\board.json");
        var path = fileReference.FullPath;

        Assert.That(store.IsStored(path), Is.True);
        Assert.That(File.Exists(boardPath), Is.False);
    }

    [Test]
    public void OnlyUploadedFilesAreServed()
    {
        var store = new ServerStorageHandler(new BoardStorageOptions(dataDirectory));
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(boardPath, "{}");

        // A path from the Windows app or an imported board isn't served, even when the file exists
        Assert.That(store.IsStored(boardPath), Is.False);
        Assert.That(store.IsStored(Path.Combine(dataDirectory, "attachments", "..", "board.json")), Is.False);
        Assert.That(store.IsStored(null), Is.False);
        Assert.That(store.IsStored(""), Is.False);
    }

    [Test]
    public void StringsUseTheChosenLanguage()
    {
        var strings = new UiStrings();
        string english = strings["dialog/cancel"];

        strings.SetLanguage("da-DK");

        Assert.That(english, Is.EqualTo("Cancel"));
        Assert.That(strings["dialog/cancel"], Is.Not.EqualTo(english));
        Assert.That(strings.CurrentLanguage, Is.EqualTo("da-DK"));
    }

    [Test]
    public void StringsFallBackSafely()
    {
        var strings = new UiStrings();
        int changes = 0;
        strings.Changed += (sender, e) => changes++;

        strings.SetLanguage("xx-unknown");
        strings.SetLanguage(null);

        Assert.That(strings.CurrentLanguage, Is.EqualTo(UiStrings.DefaultLanguage));
        Assert.That(changes, Is.Zero);
        Assert.That(strings["no/such_key"], Is.EqualTo("no/such_key"));
        Assert.That(strings.Format("dialog/tag_used_on", 3), Does.Contain("3"));
    }

    [Test]
    public void EveryLanguageHasAName()
    {
        // Each language is named in itself, so the name doesn't depend on the language that is shown
        Assert.That(UiStrings.Languages.Keys, Does.Contain(UiStrings.DefaultLanguage));
        foreach (var language in UiStrings.Languages)
        {
            Assert.That(language.Value, Is.Not.Empty, language.Key);
        }
    }

    [TestCase("en-US", 1, "What should happen to the 1 note in this column?")]
    [TestCase("en-US", 3, "What should happen to the 3 notes in this column?")]
    public void PluralFormsFollowTheNumber(string language, int count, string expected)
    {
        var strings = new UiStrings();
        strings.SetLanguage(language);

        Assert.That(strings.FormatPlural("dialog/content_delete_column_consequence", count, 0), Is.EqualTo(expected));
    }

    [Test]
    public void PluralTextsKeepTheirOtherPlaceholders()
    {
        var strings = new UiStrings();

        // {1} is the number and {0} the column name, which is filled in after the plural form is chosen
        string text = strings.FormatPlural("dialog/content_delete_column_no_space", 2, 1, "Review");

        Assert.That(text, Does.Contain("\"Review\"").And.Contain("2 notes"));
        Assert.That(text, Does.Not.Contain("[[").And.Not.Contain("{"));
    }

    [Test]
    public void EveryTextWithPluralFormsIsReadable()
    {
        // Texts the page shows with a number must never reach the user with the [[a|b]] markers left in
        string[] keys =
        {
            "dialog/delete_the_todos", "dialog/content_delete_column_consequence", "dialog/content_delete_column_no_space",
            "dialog/tag_used_on", "dialog/delete_tag_confirm", "dialog/merge_tag_confirm", "filter/button_active"
        };

        foreach (string language in UiStrings.Languages.Keys)
        {
            var strings = new UiStrings();
            strings.SetLanguage(language);
            foreach (string key in keys)
            {
                foreach (int count in new[] { 1, 2, 5 })
                {
                    Assert.That(strings.FormatPlural(key, count, 0, "a", "b", "c"), Does.Not.Contain("[["), $"{language} {key} {count}");
                }
            }
        }
    }

    [TestCase("#000000", 0.4, "#666666")]
    [TestCase("#FFFFFF", 0.4, "#FFFFFF")]
    [TestCase("#512BD4", 0.0, "#512BD4")]
    [TestCase("#512BD4", 1.0, "#FFFFFF")]
    public void LightenBlendsTowardsWhite(string color, double amount, string expected)
    {
        Assert.That(AppearanceManager.Lighten(color, amount), Is.EqualTo(expected));
    }

    [Test]
    public void UnknownAccentFallsBackToTheDefault()
    {
        Assert.That(AccentPalette.Find("#123456"), Is.SameAs(AccentPalette.Default));
        Assert.That(AccentPalette.Find(null), Is.SameAs(AccentPalette.Default));
        Assert.That(AccentPalette.Find("#2e7d32").Name, Is.EqualTo("Green"));
    }
}
