using ToDo_App.Interfaces;

namespace ToDo_App.Client;

public class ClientStorageHandler : IStorageHandler
{
    public Task<AttachedFile?> GetFile(IFileReference fileReference)
    {
        throw new NotImplementedException();
    }

    public Task<IFileReference> SaveAsync(Stream stream, string fileName)
    {
        throw new NotImplementedException();
    }

    public bool IsStored(string? path)
    {
        throw new NotImplementedException();
    }
}