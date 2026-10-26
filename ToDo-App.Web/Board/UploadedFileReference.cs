using ToDo_App.Interfaces;

namespace ToDo_App.Web.Board;

public class UploadedFileReference(string fileName, string fullPath) : IFileReference
{
    public string Name { get; } = fileName;
    public string FullPath { get; } = fullPath;

    public Stream OpenStream()
    {
        throw new NotImplementedException();
    }
}