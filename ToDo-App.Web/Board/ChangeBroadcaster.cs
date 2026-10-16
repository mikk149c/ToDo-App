using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using ToDo_App.Controller;

namespace ToDo_App.Web.Board;

/// <summary>A change as it's sent to the remote apps: its version, and the controller method that makes it, with its arguments.</summary>
public sealed record BoardChange(long Version, string Change, JsonElement[] Arguments);

/// <summary>Sends changes to the remote apps, in the order they were made.</summary>
public interface IChangeBroadcaster
{
    void Enqueue(BoardChange change);
}

/// <summary>The hub remote apps connect to. They only listen: changes come in through the API.</summary>
public sealed class BoardHub : Hub
{
}

/// <summary>
/// Sends changes over SignalR from one background loop, so they leave in version order without making the board wait
/// for the network.
/// </summary>
public sealed class SignalRBroadcaster : BackgroundService, IChangeBroadcaster
{
    private readonly Channel<BoardChange> changes = Channel.CreateUnbounded<BoardChange>(
        new UnboundedChannelOptions { SingleReader = true });
    private readonly IHubContext<BoardHub> hub;
    private readonly ILogger<SignalRBroadcaster> logger;

    public SignalRBroadcaster(IHubContext<BoardHub> hub, ILogger<SignalRBroadcaster> logger)
    {
        this.hub = hub;
        this.logger = logger;
    }

    public void Enqueue(BoardChange change)
    {
        changes.Writer.TryWrite(change);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BoardChange change in changes.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await hub.Clients.All.SendAsync(BoardProtocol.ChangedMessage, change.Version, change.Change, change.Arguments,
                    stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // An app that misses a version loads the whole board again, so one failed send is not fatal
                logger.LogWarning(e, "Could not send version {Version} to the remote apps", change.Version);
            }
        }
    }
}
