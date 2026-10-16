using System.Text.Json;

namespace ToDo_App.Controller;

/// <summary>What a ToDo-App server and its remote apps agree on: addresses, message names and JSON.</summary>
public static class BoardProtocol
{
    /// <summary>GET: the whole board and its version, as a <see cref="BoardSnapshot"/>.</summary>
    public const string SnapshotPath = "board/snapshot";

    /// <summary>
    /// POST <c>commands/{change}</c>, with the change's arguments as a JSON array, for example
    /// <c>commands/SetNoteTitle</c> with <c>["…note id…", "New title"]</c>. The changes are the abstract methods of
    /// <see cref="BoardController"/>.
    /// </summary>
    public const string CommandsPath = "commands";

    /// <summary>The SignalR hub the server sends every change through.</summary>
    public const string HubPath = "hub/board";

    /// <summary>The hub message for a change: (long version, string change, JsonElement[] arguments).</summary>
    public const string ChangedMessage = "Changed";

    public static JsonSerializerOptions Json { get; } = createOptions();

    public static string Serialize(BoardSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Json);

    /// <exception cref="JsonException">The JSON isn't a board snapshot.</exception>
    public static BoardSnapshot DeserializeSnapshot(string json) =>
        JsonSerializer.Deserialize<BoardSnapshot>(json, Json) ?? throw new JsonException("The snapshot is empty.");

    private static JsonSerializerOptions createOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

/// <summary>The whole board, and the version of the last change in it, so a remote app knows which changes to apply next.</summary>
public sealed record BoardSnapshot(long Version, ToDoBoard Board);
