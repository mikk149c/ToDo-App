using ToDo_App.Interfaces;

namespace ToDo_App;

public class LocalFileReference(string fullPath) : IFileReference
{
    private FileInfo _fileInfo = new(fullPath);
    public string Name => _fileInfo.Name;
    public string FullPath => _fileInfo.FullName;

    public Stream OpenStream()
    {
        return _fileInfo.OpenRead();
    }
}