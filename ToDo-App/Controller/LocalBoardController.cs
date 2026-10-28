namespace ToDo_App.Controller;

/// <summary>
/// A board in memory that applies every change straight away. This is how the controller has always worked: saving is
/// still up to whoever listens to BoardChanged.
/// </summary>
public class LocalBoardController : BoardController
{
}
