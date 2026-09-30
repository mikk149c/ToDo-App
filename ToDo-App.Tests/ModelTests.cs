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

    [Test]
    public void TestInsertNotes()
    {
        String firstContent = "Køb bananer\nÆbler\nCitroner";
        var ShoppingNote = new ToDoNote(firstContent);
        String secondContent = "Vælg programmeringssprog\nC#\nPython\nJava\nC";
        var ProgrammingLanguageNote = new ToDoNote(secondContent);
        String thirdContent = "Australia\nJapan\nDenmark";
        var VacationNote = new ToDoNote(thirdContent);

        board.CreateColumn("Planned");
        board.StatusColumns[0].AddNote(ShoppingNote);
        board.StatusColumns[0].AddNote(ProgrammingLanguageNote);
        board.StatusColumns[0].InsertNote(1, VacationNote);

        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(thirdContent));
        Assert.That(board.StatusColumns[0].Notes[2].Content, Is.EqualTo(secondContent));


    }

    [Test]
    public void TestMoveNote()
    {
        String firstContent = "Køb bananer\nÆbler\nCitroner";
        var ShoppingNote = new ToDoNote(firstContent);
        String secondContent = "Vælg programmeringssprog\nC#\nPython\nJava\nC";
        var ProgrammingLanguageNote = new ToDoNote(secondContent);
        String thirdContent = "Australia\nJapan\nDenmark";
        var VacationNote = new ToDoNote(thirdContent);
        String fourthContent = "Test moving notes";
        var TestMoveNotesNote = new ToDoNote(fourthContent);

        board.CreateColumn("Planned");
        board.StatusColumns[0].AddNote(ShoppingNote);
        board.StatusColumns[0].AddNote(ProgrammingLanguageNote);
        board.StatusColumns[0].AddNote(VacationNote);
        board.StatusColumns[0].AddNote(TestMoveNotesNote);

        int fromColumnIndex = 0;
        int fromPos = 3;
        int toColumnIndex = 0;
        int toPos = 1;
        board.MoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos);
        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(fourthContent));
        Assert.That(board.StatusColumns[0].Notes[2].Content, Is.EqualTo(secondContent));
        Assert.That(board.StatusColumns[0].Notes[3].Content, Is.EqualTo(thirdContent));
    }
}