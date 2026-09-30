namespace ToDo_App.Tests;

using ToDo_App;

public class Tests
{
    private ToDoBoard board;
    
    [SetUp]
    public void Setup()
    {
        board = new ToDoBoard();
    }

    [Test]
    public void TestCreateAndRemoveColumn()
    {
        board.CreateColumn("test");
        Assert.That(board.StatusColumns, Has.Count.EqualTo(1));
        board.RemoveColumn(0);
        Assert.That(board.StatusColumns, Is.Empty);
    }
}