using Microsoft.UI.Input;

namespace ToDo_App.UI.Views;

public class CursorGrid : Grid
{
    public CursorGrid(InputSystemCursorShape shape)
    {
        ProtectedCursor = InputSystemCursor.Create(shape);
    }
}

public class CursorButton : Button
{
    public CursorButton(InputSystemCursorShape shape)
    {
        ProtectedCursor = InputSystemCursor.Create(shape);
    }
}