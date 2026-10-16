using System.Text.Json;

namespace ToDo_App.Controller;

/// <summary>
/// The arguments of a change as they travel between a server and its remote apps: a JSON array, in the order of the
/// change method's parameters (see <see cref="BoardProtocol"/>).
/// </summary>
public sealed class ChangeArguments(string change, IReadOnlyList<JsonElement> arguments)
{
    /// <summary>Writes a change's arguments as JSON, to send them.</summary>
    public static JsonElement[] ToJson(params object?[] values)
    {
        return values
            .Select(value => JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object), BoardProtocol.Json))
            .ToArray();
    }

    /// <summary>Reads the argument at <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentException">The argument is missing, or isn't a <typeparamref name="T"/>.</exception>
    public T Get<T>(int index)
    {
        if (index >= arguments.Count)
        {
            throw new ArgumentException($"{change} needs more than {arguments.Count} arguments.");
        }

        try
        {
            return arguments[index].Deserialize<T>(BoardProtocol.Json)!;
        }
        catch (JsonException e)
        {
            throw new ArgumentException($"Argument {index + 1} of {change} isn't valid: {e.Message}", e);
        }
    }
}
