using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DevOpsPlatform.Application;

namespace DevOpsPlatform.Infrastructure;

public sealed class InMemoryLiveEventBus : ILiveEventBus
{
    private readonly ConcurrentDictionary<Guid, Subscriber> subscribers = new();

    public async IAsyncEnumerable<LiveUpdate> SubscribeAsync(Guid projectId, Guid environmentId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var subscriber = new Subscriber(projectId, environmentId, Channel.CreateBounded<LiveUpdate>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest }));
        subscribers[id] = subscriber;
        try
        {
            await foreach (var item in subscriber.Channel.Reader.ReadAllAsync(cancellationToken)) yield return item;
        }
        finally { subscribers.TryRemove(id, out _); }
    }

    public void Publish(Guid projectId, Guid environmentId, LiveUpdate update)
    {
        foreach (var subscriber in subscribers.Values)
            if (subscriber.ProjectId == projectId && subscriber.EnvironmentId == environmentId)
                subscriber.Channel.Writer.TryWrite(update);
    }

    private sealed record Subscriber(Guid ProjectId, Guid EnvironmentId, Channel<LiveUpdate> Channel);
}
