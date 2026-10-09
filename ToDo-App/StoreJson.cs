using System.Text.Json;

public class StoreJson<T> where T : new()
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public StoreJson(string path)
    {
        _path = path;
    }

    public T Load()
    {
        string json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
    }

    public void Save(T data)
    {
        string json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(_path, json);
    }
}
