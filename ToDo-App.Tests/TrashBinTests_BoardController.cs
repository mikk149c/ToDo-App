
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
        _controller = new BoardController();
        _columnId = _controller.CreateColumn("Planned");
        _otherColumnId = _controller.CreateColumn("In Progress");
        _noteId = _controller.CreateNote(
            _columnId, "Test note", "Test content");
    }

    [Test]
    public void RemoveColumn_MovesNotesToTrashAndRemovesColumn()
    {
        var secondNoteId = _controller.CreateNote(
            _columnId, "Second note", "Second content");

        int removedEvents = 0;
        int boardChangedEvents = 0;

        _controller.ColumnRemoved += (_, _) => removedEvents++;
        _controller.BoardChanged += (_, _) => boardChangedEvents++;

        _controller.RemoveColumn(_columnId);

        Assert.That(_controller.GetStatusColumnIds(),
            Does.Not.Contain(_columnId));

        Assert.That(_controller.GetTrashBinNotes()
            .ConvertAll(note => note.Id),
            Is.EquivalentTo(new[] { _noteId, secondNoteId }));

        Assert.That(removedEvents, Is.EqualTo(1));
        Assert.That(boardChangedEvents, Is.EqualTo(1));
    }

    [Test]
    public void EmptyTrashBin_RemovesAllNotesFromTrash()
    {
        _controller.RemoveNote(_noteId);

        Assert.That(_controller.GetTrashBinNotes(), Has.Count.EqualTo(1));

        _controller.EmptyTrashBin();

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
    }

    [Test]
    public void EmptyTrashBin_DoesNothingWhenAlreadyEmpty()
    {
        _controller.EmptyTrashBin();

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
    }

    [Test]
    public void GetTrashBinNotes_ReturnsNotesMovedToTrash()
    {
        _controller.RemoveNote(_noteId);

        var trashNotes = _controller.GetTrashBinNotes();

        Assert.That(trashNotes, Has.Count.EqualTo(1));
        Assert.That(trashNotes[0].Id, Is.EqualTo(_noteId));
        Assert.That(trashNotes[0].Title, Is.EqualTo("Test note"));
        Assert.That(trashNotes[0].Content, Is.EqualTo("Test content"));
    }

    [Test]
    public void RemoveNote_MovesNoteFromColumnToTrash()
    {
        int contentChangedEvents = 0;
        int boardChangedEvents = 0;

        _controller.ColumnContentChanged += (_, _) => contentChangedEvents++;
        _controller.BoardChanged += (_, _) => boardChangedEvents++;

        _controller.RemoveNote(_noteId);

        Assert.That(_controller.GetNotesInColumn(_columnId), Is.Empty);
        Assert.That(_controller.GetTrashBinNotes(), Has.Count.EqualTo(1));
        Assert.That(_controller.GetTrashBinNotes()[0].Id, Is.EqualTo(_noteId));

        Assert.That(contentChangedEvents, Is.EqualTo(1));
        Assert.That(boardChangedEvents, Is.EqualTo(1));
    }

    [Test]
    public void RemoveNoteFromTrashBin_PermanentlyRemovesNote()
    {
        _controller.RemoveNote(_noteId);

        _controller.RemoveNoteFromTrashBin(_noteId);

        Assert.That(_controller.GetTrashBinNotes(), Is.Empty);
        Assert.That(_controller.GetNotesInColumn(_columnId), Is.Empty);
    }

    [Test]
    public void RemoveNoteFromTrashBin_DoesNothingForUnknownId()
    {
        _controller.RemoveNote(_noteId);

        _controller.RemoveNoteFromTrashBin(Guid.NewGuid());

        Assert.That(_controller.GetTrashBinNotes(), Has.Count.EqualTo(1));
        Assert.That(_controller.GetTrashBinNotes()[0].Id, Is.EqualTo(_noteId));
    }

    [Test]
    public void RestoreNote_MovesNoteFromTrashToTargetColumn()
    {
        _controller.RemoveNote(_noteId);

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
