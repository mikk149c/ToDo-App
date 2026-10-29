using System.Text.Json;
using ToDo_App.Interfaces;

namespace ToDo_App.Client;

public class ClientStorageHandler : IStorageHandler
{
    private const string ServerUrlVariable = "TODO_SERVER_URL";
    private const string DefaultServerUrl = "http://localhost:8080/";

    private readonly HttpClient _httpClient;
    private const string UploadPath = "upload-file";
    
    private readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true };
    
    public ClientStorageHandler()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable(ServerUrlVariable) ?? DefaultServerUrl);
    }

    public async Task<FileReference> SaveAsync(Stream stream, string fileName)
    {
        using (var formData = new MultipartFormDataContent())
        {
            formData.Add(new StringContent(fileName), "name");
            formData.Add(new StreamContent(stream), "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, UploadPath)
            {
                Content = formData
            };
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var reference = JsonSerializer.Deserialize<FileReference>(json, serializerOptions);
                return reference;
            }
            else
            {
                throw new Exception(response.ToString());
            }
        }
    }

    public AttachedFile? GetFile(Guid id)
    {
        throw new NotImplementedException();
    }

    public void Delete(AttachedFile file)
    {
        throw new NotImplementedException();
    }

    public bool IsStored(string? path)
    {
        throw new NotImplementedException();
    }
}