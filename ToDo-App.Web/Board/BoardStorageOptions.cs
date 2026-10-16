namespace ToDo_App.Web.Board;

/// <summary>Where the server keeps <c>board.json</c> and uploaded attachments.</summary>
/// <param name="DataDirectory">An absolute folder. Created when it doesn't exist.</param>
public sealed record BoardStorageOptions(string DataDirectory);
