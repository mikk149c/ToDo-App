namespace ToDo_App.Tests;

using ToDo_App.Controller;

/// <summary>
/// Records every event a <see cref="BoardController"/> raises, in order, so tests can
/// check which events fired, with what arguments, and in which order.
/// </summary>
public class EventRecorder
{
    public List<(string Name, EventArgs Args)> Events { get; } = new();

    public EventRecorder(BoardController controller)
    {
        controller.ColumnCreated += (sender, e) => Events.Add((nameof(controller.ColumnCreated), e));
        controller.ColumnNameChanged += (sender, e) => Events.Add((nameof(controller.ColumnNameChanged), e));
        controller.ColumnRemoved += (sender, e) => Events.Add((nameof(controller.ColumnRemoved), e));
        controller.ColumnContentChanged += (sender, e) => Events.Add((nameof(controller.ColumnContentChanged), e));
        controller.BoardChanged += (sender, e) => Events.Add((nameof(controller.BoardChanged), e));
        controller.ColumnOrderChanged += (sender, e) => Events.Add((nameof(controller.ColumnOrderChanged), e));
        controller.TagChanged += (sender, e) => Events.Add((nameof(controller.TagChanged), e));
    }

    public List<string> Names => Events.Select(recorded => recorded.Name).ToList();

    public List<T> ArgsOf<T>() where T : EventArgs => Events.Select(recorded => recorded.Args).OfType<T>().ToList();

    public void Clear() => Events.Clear();
}
