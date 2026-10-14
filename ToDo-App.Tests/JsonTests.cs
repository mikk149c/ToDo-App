namespace ToDo_App.Tests;

using System.Text.Json;
using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Persistence: StoreJson and the controller's save/load (#60)
public class JsonTests
{
    private string jsonPath;
    private StoreJson<ToDoBoard> storeJson;

    [SetUp]
    public void Setup()
    {
        jsonPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid()}.json");
        storeJson = new StoreJson<ToDoBoard>(jsonPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }
    }

    [Test]
    public void TestSerializeAndDeserializeJson()
    {
        var board = new ToDoBoard();
        String title1 = "First note", firstContent = "Køb bananer\nÆbler\nCitroner";
        var ShoppingNote = new ToDoNote(title1, firstContent);
        String title2 = "Second note", secondContent = "Vælg programmeringssprog\nC#\nPython\nJava\nC";
        var ProgrammingLanguageNote = new ToDoNote(title2, secondContent);
        String title3 = "Third note", thirdContent = "Australia\nJapan\nDenmark";
        var VacationNote = new ToDoNote(title3, thirdContent);
        String title4 = "Fourth note", fourthContent = "Den bedste sodavand\nFanta\nCola\nSprite";
        var DenBedsteSodavandNote = new ToDoNote(title4, fourthContent);
        String title5 = "Fifth note", fifthContent = "T-shirt størrelser\nXL\nL\nM\nS";
        var TshirtStørrelserNote = new ToDoNote(title5, fifthContent);
        String title6 = "Sixth note", sixthContent = "Nyt keyboard\n Wired eller trådløs?";
        var NytKeyboardNote = new ToDoNote(title6, sixthContent);

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

    private static ToDoBoard createBoard()
    {
        var board = new ToDoBoard();
        Guid plannedId = board.CreateColumn("Planned");
        Guid progressId = board.CreateColumn("In Progress");
        board.CreateColumn("Done");
        board.AddNote(plannedId, new ToDoNote("Shopping", "Køb bananer\nÆbler",
            new AttachedFile("list.txt", Path.Combine("docs", "list.txt"))));
        board.AddNote(plannedId, new ToDoNote("Languages", "C#\nPython"));
        board.AddNote(progressId, new ToDoNote("Vacation", "Australia"));
        return board;
    }

    [Test]
    public void RoundTripPreservesColumnsNotesAndFiles()
    {
        var board = createBoard();

        storeJson.Save(board);
        var loaded = storeJson.Load();

        Assert.That(loaded.StatusColumns, Has.Count.EqualTo(board.StatusColumns.Count));
        for (int col = 0; col < board.StatusColumns.Count; col++)
        {
            StatusColumn expectedColumn = board.StatusColumns[col];
            StatusColumn loadedColumn = loaded.StatusColumns[col];
            Assert.That(loadedColumn.Id, Is.EqualTo(expectedColumn.Id));
            Assert.That(loadedColumn.Title, Is.EqualTo(expectedColumn.Title));
            Assert.That(loadedColumn.Notes, Has.Count.EqualTo(expectedColumn.Notes.Count));

            for (int pos = 0; pos < expectedColumn.Notes.Count; pos++)
            {
                ToDoNote expectedNote = expectedColumn.Notes[pos];
                ToDoNote loadedNote = loadedColumn.Notes[pos];
                Assert.That(loadedNote.Id, Is.EqualTo(expectedNote.Id));
                Assert.That(loadedNote.Title, Is.EqualTo(expectedNote.Title));
                Assert.That(loadedNote.Content, Is.EqualTo(expectedNote.Content));
                Assert.That(loadedNote.File?.Name, Is.EqualTo(expectedNote.File?.Name));
                Assert.That(loadedNote.File?.FullPath, Is.EqualTo(expectedNote.File?.FullPath));
            }
        }
        // The note without a file comes back without one
        Assert.That(loaded.StatusColumns[0].Notes[1].File, Is.Null);
    }

    [Test]
    public void RoundTripOfEmptyBoardGivesEmptyBoard()
    {
        storeJson.Save(new ToDoBoard());

        Assert.That(storeJson.Load().StatusColumns, Is.Empty);
    }

    [Test]
    public void LoadMissingFileThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => storeJson.Load());
    }

    [Test]
    public void LoadMalformedJsonThrowsJsonException()
    {
        File.WriteAllText(jsonPath, "{ \"StatusColumns\": [ ");

        Assert.Throws<JsonException>(() => storeJson.Load());
    }

    [Test]
    public void LoadFileContainingNullReturnsEmptyBoard()
    {
        File.WriteAllText(jsonPath, "null");

        var loaded = storeJson.Load();

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.StatusColumns, Is.Empty);
    }

    [Test]
    public void SaveOverwritesExistingFile()
    {
        storeJson.Save(createBoard());
        var smallerBoard = new ToDoBoard();
        smallerBoard.CreateColumn("Only column");

        storeJson.Save(smallerBoard);

        var loaded = storeJson.Load();
        Assert.That(loaded.StatusColumns.Select(column => column.Title), Is.EqualTo(new[] { "Only column" }));
    }

    // StatusColumn, ToDoNote and AttachedFile have no parameterless constructor,
    // so this checks that System.Text.Json uses their constructors from hand-written JSON
    [Test]
    public void DeserializationUsesParameterisedConstructors()
    {
        var columnId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        string json = $$"""
            {
              "StatusColumns": [
                {
                  "Id": "{{columnId}}",
                  "Title": "Planned",
                  "Notes": [
                    {
                      "Id": "{{noteId}}",
                      "Title": "Shopping",
                      "Content": "Køb bananer",
                      "File": { "Name": "list.txt", "FullPath": "docs/list.txt" }
                    }
                  ]
                }
              ]
            }
            """;
        File.WriteAllText(jsonPath, json);

        var loaded = storeJson.Load();

        StatusColumn column = loaded.StatusColumns.Single();
        Assert.That(column.Id, Is.EqualTo(columnId));
        Assert.That(column.Title, Is.EqualTo("Planned"));
        ToDoNote note = column.Notes.Single();
        Assert.That(note.Id, Is.EqualTo(noteId));
        Assert.That(note.Title, Is.EqualTo("Shopping"));
        Assert.That(note.Content, Is.EqualTo("Køb bananer"));
        Assert.That(note.File!.Name, Is.EqualTo("list.txt"));
        Assert.That(note.File.FullPath, Is.EqualTo("docs/list.txt"));
    }

    [Test]
    public void ControllerSaveThenLoadRestoresBoard()
    {
        var original = new BoardController();
        Guid plannedId = original.CreateColumn("Planned");
        Guid progressId = original.CreateColumn("In Progress");
        Guid noteId = original.CreateNote(progressId, "Vacation", "Australia");
        original.SaveBoardToJson(jsonPath);

        var loaded = new BoardController();
        loaded.LoadBoardFromJson(jsonPath);

        Assert.That(loaded.GetStatusColumnIds(), Is.EqualTo(new[] { plannedId, progressId }));
        Assert.That(loaded.GetStatusColumnName(progressId), Is.EqualTo("In Progress"));
        Assert.That(loaded.GetNotesInColumn(progressId).Single().Id, Is.EqualTo(noteId));
    }

    [Test]
    public void LoadBoardFromJsonReplacesBoardAndRaisesColumnCreatedPerColumn()
    {
        var savedBoard = createBoard();
        storeJson.Save(savedBoard);
        var controller = new BoardController();
        Guid oldColumnId = controller.CreateColumn("Old column");
        var recorder = new EventRecorder(controller);

        controller.LoadBoardFromJson(jsonPath);

        var savedIds = savedBoard.StatusColumns.Select(column => column.Id).ToList();
        Assert.That(controller.GetStatusColumnIds(), Is.EqualTo(savedIds));
        Assert.That(controller.GetStatusColumnIds(), Does.Not.Contain(oldColumnId));

        var created = recorder.ArgsOf<ColumnCreatedEventArgs>();
        Assert.That(created.Select(e => e.ColumnId), Is.EqualTo(savedIds));
        Assert.That(created.Select(e => e.Name), Is.EqualTo(savedBoard.StatusColumns.Select(column => column.Title)));
        Assert.That(created[0].Notes.Select(note => note.Id), Is.EqualTo(savedBoard.StatusColumns[0].Notes.Select(note => note.Id)));
    }
}
