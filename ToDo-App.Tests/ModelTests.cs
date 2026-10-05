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
        Guid id = board.CreateColumn("test");
        Assert.That(board.StatusColumns, Has.Count.EqualTo(1));
        board.RemoveColumn(id);
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
        Guid id = Guid.NewGuid();
        Assert.Throws<KeyNotFoundException>(() => board.RemoveColumn(id));
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
        Guid wrongId = Guid.NewGuid();
        Assert.Throws<KeyNotFoundException>(() => board.RemoveNote(wrongId));
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

    [Test]
    public void TestSeveralFunctions_AddNote_MoveNote_RemoveNote_and_RemoveColumn()
    {
        String firstContent = "Køb bananer\nÆbler\nCitroner";
        var ShoppingNote = new ToDoNote(firstContent);
        String secondContent = "Vælg programmeringssprog\nC#\nPython\nJava\nC";
        var ProgrammingLanguageNote = new ToDoNote(secondContent);
        String thirdContent = "Australia\nJapan\nDenmark";
        var VacationNote = new ToDoNote(thirdContent);
        String fourthContent = "Den bedste sodavand\nFanta\nCola\nSprite";
        var DenBedsteSodavandNote = new ToDoNote(fourthContent);
        String fifthContent = "T-shirt størrelser\nXL\nL\nM\nS";
        var TshirtStørrelserNote = new ToDoNote(fifthContent);
        String sixthContent = "Nyt keyboard\n Wired eller trådløs?";
        var NytKeyboardNote = new ToDoNote(sixthContent);

        Guid plannedColumnId = board.CreateColumn("Planned");
        board.StatusColumns[0].AddNote(ShoppingNote);
        Guid deleteNoteId = board.AddNote(plannedColumnId, ProgrammingLanguageNote);
        board.StatusColumns[0].AddNote(VacationNote);
        board.StatusColumns[0].AddNote(DenBedsteSodavandNote);
        
        Guid progressColumnId = board.CreateColumn("In Progress");
        board.StatusColumns[1].AddNote(TshirtStørrelserNote);

        //FIRST CHECK
        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(secondContent));
        Assert.That(board.StatusColumns[0].Notes[2].Content, Is.EqualTo(thirdContent));
        Assert.That(board.StatusColumns[0].Notes[3].Content, Is.EqualTo(fourthContent));
        
        Assert.That(board.StatusColumns[1].Notes[0].Content, Is.EqualTo(fifthContent));

        //DELETE SECOND NOTE FROM FIRST COLUMN AND CHECK
        board.RemoveNote(deleteNoteId);
        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(thirdContent));
        Assert.That(board.StatusColumns[0].Notes[2].Content, Is.EqualTo(fourthContent));
        
        Assert.That(board.StatusColumns[1].Notes[0].Content, Is.EqualTo(fifthContent));
        Assert.That(board.StatusColumns[0].Notes.Count, Is.EqualTo(3));

        //MOVE NOTE AND CHECK
        int moveNoteFromColumnIndex = 0, moveNoteAtIndex = 0, moveNoteToColumnIndex = 1, moveNoteToIndex = 0;
        board.MoveNote(moveNoteFromColumnIndex, moveNoteAtIndex, moveNoteToColumnIndex, moveNoteToIndex);

        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(thirdContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(fourthContent));
        Assert.That(board.StatusColumns[0].Notes.Count, Is.EqualTo(2));

        Assert.That(board.StatusColumns[1].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[1].Notes[1].Content, Is.EqualTo(fifthContent));
        Assert.That(board.StatusColumns[1].Notes.Count, Is.EqualTo(2));

        //REMOVE FIRST COLUMN AND CHECK THAT SECOND COLUMN IS NOW THE FIRST
        board.RemoveColumn(plannedColumnId);
        Assert.That(board.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(board.StatusColumns[0].Notes[1].Content, Is.EqualTo(fifthContent));
        Assert.That(board.StatusColumns[0].Notes.Count, Is.EqualTo(2));
        Assert.That(board.StatusColumns.Count, Is.EqualTo(1));
    }

    [Test]
    public void TestMoveNoteRightAndLeft()
    {
        String firstContent = "Køb bananer\nÆbler\nCitroner";
        var ShoppingNote = new ToDoNote(firstContent);
        String secondContent = "Vælg programmeringssprog\nC#\nPython\nJava\nC";
        var ProgrammingLanguageNote = new ToDoNote(secondContent);

        board.CreateColumn("Planned");
        board.StatusColumns[0].AddNote(ShoppingNote);

        board.CreateColumn("In Progress");
        board.StatusColumns[1].AddNote(ProgrammingLanguageNote);

        Assert.That(board.StatusColumns[0].Notes[0], Is.EqualTo(ShoppingNote));
        Assert.That(board.StatusColumns[1].Notes[0], Is.EqualTo(ProgrammingLanguageNote));

        board.MoveNoteRight(ShoppingNote.Id);

        Assert.That(board.StatusColumns[1].Notes[0], Is.EqualTo(ShoppingNote));
        Assert.That(board.StatusColumns[1].Notes[1], Is.EqualTo(ProgrammingLanguageNote));
        
        board.MoveNoteLeft(ProgrammingLanguageNote.Id);
        
        Assert.That(board.StatusColumns[0].Notes[0], Is.EqualTo(ProgrammingLanguageNote));
        Assert.That(board.StatusColumns[1].Notes[0], Is.EqualTo(ShoppingNote));
    }
}