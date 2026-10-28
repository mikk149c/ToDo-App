namespace ToDo_App.Interfaces;

public interface IStorageHandler
{
    public Task<FileReference> SaveAsync(Stream stream, string fileName);
    public bool IsStored(string? path);
}