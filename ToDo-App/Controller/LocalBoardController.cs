using ToDo_App.Interfaces;

namespace ToDo_App.Controller;

/// <summary>
/// A board in memory that applies every change straight away. This is how the controller has always worked: saving is
/// still up to whoever listens to BoardChanged.
/// <param name="storageHandler">Handles saving/loading attached files. By default, a LocalStorageHandler, but can be overridden.</param>
/// </summary>
public class LocalBoardController(IStorageHandler? storageHandler = null) : BoardController(storageHandler ?? new LocalStorageHandler());


public class LocalStorageHandler() : IStorageHandler
{
    public Task<IFileReference> SaveAsync(Stream stream, string fileName)
    {
        throw new NotImplementedException();
    }

    public bool IsStored(string? path)
    {
        throw new NotImplementedException();
    }
}