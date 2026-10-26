namespace ToDo_App.Interfaces;

public interface IStorageHandler
{
    Task<AttachedFile?> GetFile(IFileReference fileReference);
    public Task<IFileReference> SaveAsync(Stream stream, string fileName);
    public bool IsStored(string? path);
}