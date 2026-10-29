namespace ToDo_App.Interfaces;

public interface IStorageHandler
{
    public Task<FileReference> SaveAsync(Stream stream, string fileName);
    public AttachedFile? GetFile(Guid id);
    public void Delete(AttachedFile file);
    public bool IsStored(string? path);
}