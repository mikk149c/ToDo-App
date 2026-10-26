namespace ToDo_App.Interfaces;

public interface IFileReference
{
    public string Name { get; }
    public string FullPath { get; }
    public Stream OpenStream();
}