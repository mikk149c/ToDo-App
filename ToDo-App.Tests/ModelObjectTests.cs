namespace ToDo_App.Tests;

using ToDo_App;

// StatusColumn, ToDoNote and AttachedFile on their own (#57)
public class ModelObjectTests
{
    private StatusColumn column;
    private ToDoNote firstNote;
    private ToDoNote secondNote;
    private ToDoNote newNote;

    [SetUp]
    public void Setup()
    {
        column = new StatusColumn("Planned");
        firstNote = new ToDoNote("First note", "Køb bananer");
        secondNote = new ToDoNote("Second note", "Vælg programmeringssprog");
        newNote = new ToDoNote("New note", "Australia");
        column.AddNote(firstNote);
        column.AddNote(secondNote);
    }

    [Test]
    public void ToDoNoteConstructorSetsTitleContentAndFile()
    {
        var file = new AttachedFile(Guid.NewGuid(), "list.txt", Path.Combine("docs", "list.txt"));

        var note = new ToDoNote("Shopping", "Køb bananer", file);

        Assert.That(note.Title, Is.EqualTo("Shopping"));
        Assert.That(note.Content, Is.EqualTo("Køb bananer"));
        Assert.That(note.File, Is.SameAs(file));
    }

    [Test]
    public void ToDoNoteConstructorWithoutFileHasNoFile()
    {
        var note = new ToDoNote("Shopping", "Køb bananer");

        Assert.That(note.File, Is.Null);
    }

    [Test]
    public void ToDoNoteConstructorGeneratesUniqueIds()
    {
        var ids = Enumerable.Range(0, 100).Select(i => new ToDoNote($"Note {i}", "").Id).ToList();

        Assert.That(ids, Is.Unique);
        Assert.That(ids, Has.None.EqualTo(Guid.Empty));
    }

    [Test]
    public void AttachedFileConstructorSetsNameAndFullPath()
    {
        string fullPath = Path.Combine("docs", "list.txt");

        var file = new AttachedFile(Guid.NewGuid(), "list.txt", fullPath);

        Assert.That(file.Name, Is.EqualTo("list.txt"));
        Assert.That(file.FullPath, Is.EqualTo(fullPath));
    }

    [Test]
    public void StatusColumnConstructorSetsTitleIdAndEmptyNotes()
    {
        var newColumn = new StatusColumn("Done");

        Assert.That(newColumn.Title, Is.EqualTo("Done"));
        Assert.That(newColumn.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(newColumn.Id, Is.Not.EqualTo(column.Id));
        Assert.That(newColumn.Notes, Is.Empty);
    }

    [Test]
    public void InsertNoteAtStartPutsNoteFirst()
    {
        column.InsertNote(0, newNote);

        Assert.That(column.Notes, Is.EqualTo(new[] { newNote, firstNote, secondNote }));
    }

    [Test]
    public void InsertNoteInMiddleKeepsOtherNotesInOrder()
    {
        column.InsertNote(1, newNote);

        Assert.That(column.Notes, Is.EqualTo(new[] { firstNote, newNote, secondNote }));
    }

    [Test]
    public void InsertNoteAtEndAppendsNote()
    {
        column.InsertNote(2, newNote);

        Assert.That(column.Notes, Is.EqualTo(new[] { firstNote, secondNote, newNote }));
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void InsertNoteOutOfRangeThrowsAndLeavesNotesUnchanged(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => column.InsertNote(index, newNote));
        Assert.That(column.Notes, Is.EqualTo(new[] { firstNote, secondNote }));
    }

    [Test]
    public void RemoveNoteRemovesOnlyThatNote()
    {
        column.RemoveNote(0);

        Assert.That(column.Notes, Is.EqualTo(new[] { secondNote }));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void RemoveNoteOutOfRangeThrowsAndLeavesNotesUnchanged(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => column.RemoveNote(index));
        Assert.That(column.Notes, Is.EqualTo(new[] { firstNote, secondNote }));
    }
}
