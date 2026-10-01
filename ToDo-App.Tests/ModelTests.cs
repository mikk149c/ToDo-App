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
    public void TestMoveNoteInSameColumn()
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

        int fromColumnIndex = 0, fromPos = 3, toColumnIndex = 0, toPos = 1;
        board.MoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos);
        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(fourthContent));
        Assert.That(board.StatusColumns[0].Notes[2].Content, Is.EqualTo(secondContent));
        Assert.That(board.StatusColumns[0].Notes[3].Content, Is.EqualTo(thirdContent));
    }
    
    [Test]
    public void TestMoveNoteToDifferentColumn()
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
        
        board.CreateColumn("In Progress");

        int fromColumnIndex = 0, fromPos = 3, toColumnIndex = 1, toPos = 0;
        board.MoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos);
        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(secondContent));
        Assert.That(board.StatusColumns[0].Notes[2].Content, Is.EqualTo(thirdContent));
        
        Assert.That(board.StatusColumns[1].Notes[0].Content, Is.EqualTo(fourthContent));
    }

    [Test]
    public void RemoveInvalidColumnThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => board.RemoveColumn(0));
    }

    [Test]
    public void MoveInvalidColumnThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => board.MoveColumn(0,1));
    }
    
    [Test]
    public void RemoveInvalidNoteThrowsException()
    {
        board.CreateColumn("test");
        Assert.Throws<ArgumentOutOfRangeException>(() => board.StatusColumns[0].RemoveNote(0));
    }
    
    [Test]
    public void InsertNoteAtEndWorks()
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
        
        Assert.DoesNotThrow(() => board.StatusColumns[0].InsertNote(2, VacationNote));
        
        Assert.That(board.StatusColumns[0].Notes[0], Is.EqualTo(ShoppingNote));
        Assert.That(board.StatusColumns[0].Notes[1], Is.EqualTo(ProgrammingLanguageNote));
        Assert.That(board.StatusColumns[0].Notes[2], Is.EqualTo(VacationNote));
    }
    
}