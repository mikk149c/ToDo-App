using ToDo_App.Interfaces;

namespace ToDo_App.Client;

public class ClientStorageHandler : IStorageHandler
{
    private static readonly HttpClient HttpClient = new();
    private const string UploadPath = "upload-file";

    public async Task<IFileReference> SaveAsync(Stream stream, string fileName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, UploadPath);

        var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(stream), "file", fileName);
        request.Content = formData;
        
        Console.WriteLine($"Uploading {fileName} ...");
        var response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            // TODO: return proper reference
            return new LocalFileReference("");
        }
        else
        {
            throw new Exception(response.ToString());
        }
    }

    public bool IsStored(string? path)
    {
        throw new NotImplementedException();
    }
}