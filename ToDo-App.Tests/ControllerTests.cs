namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

public class ControllerTests
{
    private BoardController controller;

    [SetUp]
    public void Setup()
    {
        controller = new BoardController();
    }

    [Test]
    public void RemoveColumnRemovesColumnAndRaisesEvent()
    {
        Guid plannedId = controller.CreateColumn("Planned");
        Guid progressId = controller.CreateColumn("In Progress");
        controller.CreateNote(plannedId, "First note", "Køb bananer");

        Guid? removedId = null;
        controller.ColumnRemoved += (sender, e) => removedId = ((ColumnRemovedEventArgs)e).ColumnId;

        controller.RemoveColumn(plannedId);

        Assert.That(removedId, Is.EqualTo(plannedId));
        Assert.That(controller.GetStatusColumnIds(), Is.EqualTo(new List<Guid> { progressId }));
        Assert.That(controller.GetNotesInColumn(progressId), Is.Empty);
    }

    [Test]
    public void RemoveColumnWithMoveKeepsNotesInTargetColumn()
    {
        Guid plannedId = controller.CreateColumn("Planned");
        Guid progressId = controller.CreateColumn("In Progress");
        Guid existingNoteId = controller.CreateNote(progressId, "Existing note", "Australia");
        Guid firstMovedId = controller.CreateNote(plannedId, "First note", "Køb bananer");
        Guid secondMovedId = controller.CreateNote(plannedId, "Second note", "Vælg programmeringssprog");

        Guid? changedColumnId = null;
        Guid? removedId = null;
        controller.ColumnContentChanged += (sender, e) => changedColumnId = ((ColumnContentChangedEventArgs)e).ColumnId;
        controller.ColumnRemoved += (sender, e) => removedId = ((ColumnRemovedEventArgs)e).ColumnId;

        controller.RemoveColumn(plannedId, progressId);

        Assert.That(changedColumnId, Is.EqualTo(progressId));
        Assert.That(removedId, Is.EqualTo(plannedId));
        Assert.That(controller.GetStatusColumnIds(), Is.EqualTo(new List<Guid> { progressId }));

        List<ToDoNote> notes = controller.GetNotesInColumn(progressId);
        Assert.That(notes, Has.Count.EqualTo(3));
        Assert.That(notes[0].Id, Is.EqualTo(existingNoteId));
        Assert.That(notes[1].Id, Is.EqualTo(firstMovedId));
        Assert.That(notes[2].Id, Is.EqualTo(secondMovedId));
    }

    [Test]
    public void RemoveColumnWithMoveToSameColumnThrowsException()
    {
        Guid plannedId = controller.CreateColumn("Planned");
        controller.CreateNote(plannedId, "First note", "Køb bananer");

        Assert.Throws<ArgumentException>(() => controller.RemoveColumn(plannedId, plannedId));
        Assert.That(controller.GetNotesInColumn(plannedId), Has.Count.EqualTo(1));
    }

    [Test]
    public void RemoveColumnWithMoveToInvalidColumnThrowsException()
    {
        Guid plannedId = controller.CreateColumn("Planned");
        controller.CreateNote(plannedId, "First note", "Køb bananer");

        Assert.Throws<KeyNotFoundException>(() => controller.RemoveColumn(plannedId, Guid.NewGuid()));
        Assert.That(controller.GetNotesInColumn(plannedId), Has.Count.EqualTo(1));
    }
}
