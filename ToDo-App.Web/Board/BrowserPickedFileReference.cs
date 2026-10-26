using Microsoft.AspNetCore.Components.Forms;
using ToDo_App.Interfaces;

namespace ToDo_App.Web.Board;

public class BrowserPickedFileReference(InputFileChangeEventArgs eventArgs) : IFileReference
{
    public string Name => eventArgs.File.Name;
    public string FullPath => "";

    public Stream OpenStream()
    {
        return eventArgs.File.OpenReadStream(ServerStorageHandler.MaxFileSize);
    }
}