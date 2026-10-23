using Microsoft.UI.Dispatching;
using ToDo_App.Client;
using ToDo_App.Interfaces;

namespace ToDo_App.UI.Board;

/// <summary>
/// How the page changes the board: <c>changes.Send(board => board.SetNoteTitle(id, title), id)</c>.
/// </summary>
/// <remarks>
/// <para>The server's board (Remote) is slow, so a change to it is sent in the background, one at a time and in the
/// order they were made, and the Ids given are grayed out (<see cref="Pending"/>) until an event mentions them. The
/// page's event handlers let them go. A change the server refuses raises <see cref="Failed"/>.</para>
/// <para>The board on this PC (Local) is changed straight away, and its exceptions reach the caller as before.</para>
/// <para>Read anything from the page (a text box, say) before calling, not in the change: the change runs on another
/// thread.</para>
/// </remarks>
public sealed class BoardChanges(Func<ITodoBoard> currentBoard, DispatcherQueue ui)
{
    /// <summary>
    /// When an Id no event will mention (a removed todo, the trash bin) is let go. The server accepted the change, so
    /// it's there.
    /// </summary>
    public static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(10);

    private Task queue = Task.CompletedTask;
    private readonly HashSet<DispatcherQueueTimer> settleTimers = new(); // kept until they fire, so they aren't collected

    /// <summary>The server refused a change. Raised on the UI thread. Nothing changed on the board.</summary>
    public event EventHandler<Exception>? Failed;

    /// <param name="change">The change, made on the board it's given.</param>
    /// <param name="pendingIds">What to gray out until the server confirms the change.</param>
    public void Send(Action<ITodoBoard> change, params Guid[] pendingIds)
    {
        ITodoBoard board = currentBoard();
        if (board is not RemoteBoardController)
        {
            change(board);
            return;
        }

        Pending.Add(pendingIds);
        queue = queue.ContinueWith(_ =>
        {
            try
            {
                change(board);
                ui.TryEnqueue(() => settleLater(pendingIds));
            }
            catch (Exception exception)
            {
                ui.TryEnqueue(() =>
                {
                    Pending.Remove(pendingIds);
                    Failed?.Invoke(this, exception);
                });
            }
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private void settleLater(Guid[] ids)
    {
        if (ids.Length == 0)
        {
            return;
        }

        DispatcherQueueTimer timer = ui.CreateTimer();
        timer.Interval = SettleTime;
        timer.IsRepeating = false;
        timer.Tick += (sender, e) =>
        {
            settleTimers.Remove(timer);
            Pending.Remove(ids);
        };
        settleTimers.Add(timer);
        timer.Start();
    }
}
