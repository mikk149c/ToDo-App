using ToDo_App.Interfaces;

namespace ToDo_App.Web.Board;

public class ServerStorageHandler(BoardStorageOptions options) : IStorageHandler
{
    public const long MaxFileSize = 50 * 1024 * 1024;

    private readonly string root = Path.GetFullPath(Path.Combine(options.DataDirectory, "attachments"));
    
    /// <summary>Saves an uploaded file and returns its full path on the server.</summary>
    public async Task<FileReference> SaveAsync(Stream stream, string fileName)
    {
        var fileGuid = Guid.NewGuid();
        
        // Only the name is kept, so a name like "..\board.json" can't write outside the folder
        string safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "attachment";
        }
        
        string folder = Path.Combine(root, fileGuid.ToString());
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, safeName);

        await using FileStream file = File.Create(path);
        await stream.CopyToAsync(file);

        var uploadedFileReference = new FileReference
        {
            type = FileReference.FileReferenceType.Uploaded,
            guid = fileGuid,
            name = fileName,
            fullPath = path
        };
        
        return uploadedFileReference;
    }

    public AttachedFile? GetFile(Guid id)
    {
        var folder = Path.Combine(root, id.ToString());
        
        if (!Directory.Exists(folder))
            return null;

        var files = Directory.EnumerateFiles(folder);

        return files.Select(file => new AttachedFile(id, file, Path.Combine(folder, file))).FirstOrDefault();
    }

    public void Delete(AttachedFile file)
    {
        var folder = Path.Combine(root, file.Guid.ToString());
        Directory.Delete(folder, true);
    }

    /// <summary>
    /// True when the path is a file uploaded through the browser. Other paths, such as attachments added in the
    /// Windows app, point somewhere on a PC and are never served, whatever an imported board says.
    /// </summary>
    public bool IsStored(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               && File.Exists(fullPath);
    }
}