namespace ToDo_App.Interfaces;

public interface IStorageHandler
{
    public Task<IFileReference> SaveAsync(Stream stream, string fileName);
    public bool IsStored(string? path);
}