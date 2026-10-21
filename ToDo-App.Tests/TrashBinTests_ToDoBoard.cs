
using NUnit.Framework;
using System;
using System.Collections.Generic;
using ToDo_App;

namespace ToDo_App.Tests;

[TestFixture]
public class ToDoBoardTrashBinTests
{
    private ToDoBoard _board = null!;
    private Guid _columnId;
    private Guid _otherColumnId;
    private Guid _noteId;
    private ToDoNote _note = null!;

    [SetUp]
    public void SetUp()
    {
        _board = new ToDoBoard();
        _columnId = _board.CreateColumn("Planned");
        _otherColumnId = _board.CreateColumn("In Progress");

        _note = new ToDoNote("Test note", "Test content");
        _noteId = _board.AddNote(_columnId, _note);
    }

    [Test]
    public void AddNoteToTrashBin_AddsNoteWithoutRemovingItFromColumn()
    {
        _board.AddNoteToTrashBin(_noteId);

        Assert.That(_board.TrashBin.Notes, Has.Count.EqualTo(1));
        Assert.That(_board.TrashBin.Notes[0], Is.SameAs(_note));
        Assert.That(_board.StatusColumns[0].Notes, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddColumnNotesToTrashBin_AddsAllNotesFromColumn()
    {
        var secondNote = new ToDoNote("Second note", "Second content");
        _board.AddNote(_columnId, secondNote);

        _board.AddColumnNotesToTrashBin(_columnId);

        Assert.That(_board.TrashBin.Notes, Has.Count.EqualTo(2));
        Assert.That(_board.TrashBin.Notes, Does.Contain(_note));
        Assert.That(_board.TrashBin.Notes, Does.Contain(secondNote));

        // The method copies notes to the bin; it does not remove them.
        Assert.That(_board.StatusColumns[0].Notes, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddColumnNotesToTrashBin_DoesNothingWhenColumnIsEmpty()
    {
        var emptyColumnId = _board.CreateColumn("Empty");

        _board.AddColumnNotesToTrashBin(emptyColumnId);

        Assert.That(_board.TrashBin.Notes, Is.Empty);
    }

    [Test]
    public void EmptyTrashBin_RemovesAllNotes()
    {
        _board.AddNoteToTrashBin(_noteId);

        _board.EmptyTrashBin();

        Assert.That(_board.TrashBin.Notes, Is.Empty);
    }

    [Test]
    public void EmptyTrashBin_DoesNothingWhenBinIsAlreadyEmpty()
    {
        _board.EmptyTrashBin();

        Assert.That(_board.TrashBin.Notes, Is.Empty);
    }

    [Test]
    public void RemoveNoteFromTrashBin_RemovesMatchingNote()
    {
        _board.AddNoteToTrashBin(_noteId);

        _board.RemoveNoteFromTrashBin(_noteId);

        Assert.That(_board.TrashBin.Notes, Is.Empty);
    }

    [Test]
    public void RemoveNoteFromTrashBin_DoesNothingWhenNoteIsNotFound()
    {
        var unknownId = Guid.NewGuid();
        _board.AddNoteToTrashBin(_noteId);

        _board.RemoveNoteFromTrashBin(unknownId);

        Assert.That(_board.TrashBin.Notes, Has.Count.EqualTo(1));
        Assert.That(_board.TrashBin.Notes[0].Id, Is.EqualTo(_noteId));
    }

    [Test]
    public void RestoreNoteFromTrashBin_MovesNoteToTargetColumn()
    {
        _board.AddNoteToTrashBin(_noteId);
        _board.RemoveNote(_noteId);

        _board.RestoreNoteFromTrashBin(_noteId, _otherColumnId);

        Assert.That(_board.TrashBin.Notes, Is.Empty);
        Assert.That(_board.StatusColumns[0].Notes, Is.Empty);
        Assert.That(_board.StatusColumns[1].Notes, Has.Count.EqualTo(1));
        Assert.That(_board.StatusColumns[1].Notes[0], Is.SameAs(_note));
    }

    [Test]
    public void RestoreNoteFromTrashBin_DoesNothingWhenNoteIsNotFound()
    {
        _board.AddNoteToTrashBin(_noteId);
        _board.RemoveNote(_noteId);

        _board.RestoreNoteFromTrashBin(Guid.NewGuid(), _otherColumnId);

        Assert.That(_board.TrashBin.Notes, Has.Count.EqualTo(1));
        Assert.That(_board.StatusColumns[1].Notes, Is.Empty);
    }

    [Test]
    public void RestoreNoteFromTrashBin_ThrowsWhenTargetColumnDoesNotExist()
    {
        _board.AddNoteToTrashBin(_noteId);
        _board.RemoveNote(_noteId);

        Assert.Throws<KeyNotFoundException>(() =>
            _board.RestoreNoteFromTrashBin(_noteId, Guid.NewGuid()));

        Assert.That(_board.TrashBin.Notes, Has.Count.EqualTo(1));
    }
}
