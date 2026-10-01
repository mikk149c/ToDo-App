namespace ToDo_App.Tests;

public class JsonTests
{
    private StoreJson<ToDoBoard> storeJson;
    [SetUp]
    public void Setup()
    {
        storeJson = new StoreJson<ToDoBoard>("board.json");
    }
    
    [Test]
    public void TestSerializeAndDeserializeJson()
    {
        var board = new ToDoBoard();
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

        board.CreateColumn("Planned");
        board.StatusColumns[0].AddNote(ShoppingNote);
        board.StatusColumns[0].AddNote(ProgrammingLanguageNote);
        board.StatusColumns[0].AddNote(VacationNote);
        board.StatusColumns[0].AddNote(DenBedsteSodavandNote);
        
        board.CreateColumn("In Progress");
        board.StatusColumns[1].AddNote(TshirtStørrelserNote);
        
        storeJson.Save(board);

        var loadedBoard = storeJson.Load();
        
        Assert.That(loadedBoard.StatusColumns[0].Notes[0].Content, Is.EqualTo(firstContent));
        Assert.That(loadedBoard.StatusColumns[0].Notes[1].Content, Is.EqualTo(secondContent));
        Assert.That(loadedBoard.StatusColumns[0].Notes[2].Content, Is.EqualTo(thirdContent));
        Assert.That(loadedBoard.StatusColumns[0].Notes[3].Content, Is.EqualTo(fourthContent));
        
        Assert.That(loadedBoard.StatusColumns[1].Notes[0].Content, Is.EqualTo(fifthContent));
    }
}