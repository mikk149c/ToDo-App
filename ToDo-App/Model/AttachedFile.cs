namespace ToDo_App;

public class AttachedFile
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    
    public AttachedFile(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }
}