
using NUnit.Framework;
using System;
using ToDo_App.Controller;

namespace ToDo_App.Tests;

[TestFixture]
public class BoardControllerTrashBinTests
{
    private BoardController _controller = null!;
    private Guid _columnId;
    private Guid _otherColumnId;
    private Guid _noteId;

    [SetUp]
    public void SetUp()
    {
        _controller = new LocalBoardController();
        _columnId = _controller.CreateColumn("Planned");
        _otherColumnId = _controller.CreateColumn("In Progress");
        _noteId = _controller.CreateNote(
            _columnId, "Test note", "Test content");
    }

    [Test]
    public void DeleteColumn_MovesNotesToTrashAndRemovesColumn()
    {
        var secondNoteId = _controller.CreateNote(
            _columnId, "Second note", "Second content");

        int removedEvents = 0;
        int boardChangedEvents = 0;

        _controller.ColumnRemoved += (_, _) => removedEvents++;
        _controller.BoardChanged += (_, _) => boardChangedEvents++;

        _controller.DeleteColumn(_columnId);

        Assert.That(_controller.GetStatusColumnIds(),
            Does.Not.Contain(_columnId));

        Assert.That(_controller.GetTrashBinNotes()
            .ConvertAll(note => note.Id),
            Is.EquivalentTo(new[] { _noteId, secondNoteId }));

        Assert.That(removedEvents, Is.EqualTo(1));
        Assert.That(boardChangedEvents, Is.EqualTo(1));
    }

    [Test]
    public void EmptyTrash_RemovesAllNotesFromTrash()
    {
        _controller.TrashNote(_noteId);

        Assert.That(_controller.GetTrashBinNotes(), Has.Count.EqualTo(1));

        _controller.EmptyTrash();

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
    }

    [Test]
    public void EmptyTrash_DoesNothingWhenAlreadyEmpty()
    {
        _controller.EmptyTrash();

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
    }

    [Test]
    public void GetTrashBinNotes_ReturnsNotesMovedToTrash()
    {
        _controller.TrashNote(_noteId);

        var trashNotes = _controller.GetTrashBinNotes();

        Assert.That(trashNotes, Has.Count.EqualTo(1));
        Assert.That(trashNotes[0].Id, Is.EqualTo(_noteId));
        Assert.That(trashNotes[0].Title, Is.EqualTo("Test note"));
        Assert.That(trashNotes[0].Content, Is.EqualTo("Test content"));
    }

    [Test]
    public void TrashNote_MovesNoteFromColumnToTrash()
    {
        int contentChangedEvents = 0;
        int boardChangedEvents = 0;

        _controller.ColumnContentChanged += (_, _) => contentChangedEvents++;
        _controller.BoardChanged += (_, _) => boardChangedEvents++;

        _controller.TrashNote(_noteId);

        Assert.That(_controller.GetNotesInColumn(_columnId), Is.Empty);
        Assert.That(_controller.GetTrashBinNotes(), Has.Count.EqualTo(1));
        Assert.That(_controller.GetTrashBinNotes()[0].Id, Is.EqualTo(_noteId));

        Assert.That(contentChangedEvents, Is.EqualTo(1));
        Assert.That(boardChangedEvents, Is.EqualTo(1));
    }

    [Test]
    public void DeleteNote_PermanentlyRemovesNote()
    {
        _controller.TrashNote(_noteId);

        _controller.DeleteNote(_noteId);

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
        Assert.That(_controller.GetNotesInColumn(_columnId), Is.Empty);
    }

    [Test]
    public void DeleteNote_DoesNothingForUnknownId()
    {
        _controller.TrashNote(_noteId);

        _controller.DeleteNote(Guid.NewGuid());

        Assert.That(_controller.GetTrashBinNotes(), Has.Count.EqualTo(1));
        Assert.That(_controller.GetTrashBinNotes()[0].Id, Is.EqualTo(_noteId));
    }

    [Test]
    public void RestoreNote_MovesNoteFromTrashToTargetColumn()
    {
        _controller.TrashNote(_noteId);

        int contentChangedEvents = 0;
        int boardChangedEvents = 0;

        _controller.ColumnContentChanged += (_, _) => contentChangedEvents++;
        _controller.BoardChanged += (_, _) => boardChangedEvents++;

        _controller.RestoreNote(_noteId, _otherColumnId);

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
        Assert.That(_controller.GetNotesInColumn(_columnId), Is.Empty);
        Assert.That(_controller.GetNotesInColumn(_otherColumnId), Has.Count.EqualTo(1));

        var restoredNote = _controller.GetNotesInColumn(_otherColumnId)[0];

        Assert.That(restoredNote.Id, Is.EqualTo(_noteId));
        Assert.That(restoredNote.Title, Is.EqualTo("Test note"));
        Assert.That(restoredNote.Content, Is.EqualTo("Test content"));

        Assert.That(contentChangedEvents, Is.EqualTo(1));
        Assert.That(boardChangedEvents, Is.EqualTo(1));
    }
}
