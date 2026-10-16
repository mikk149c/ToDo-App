namespace ToDo_App.Web.Board;

/// <summary>
/// Keeps files attached in the browser on the server, each in its own folder so two files with the same
/// name don't clash. A todo stores the file's full path, like an attachment added in the Windows app.
/// </summary>
public sealed class AttachmentStore
{
    public const long MaxFileSize = 50 * 1024 * 1024;

    private readonly string root;

    public AttachmentStore(BoardStorageOptions options)
    {
        root = Path.GetFullPath(Path.Combine(options.DataDirectory, "attachments"));
    }

    /// <summary>Saves an uploaded file and returns its full path on the server.</summary>
    public async Task<string> SaveAsync(Stream content, string fileName)
    {
        // Only the name is kept, so a name like "..\board.json" can't write outside the folder
        string safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "attachment";
        }

        string folder = Path.Combine(root, Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, safeName);

        await using FileStream file = File.Create(path);
        await content.CopyToAsync(file);
        return path;
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
