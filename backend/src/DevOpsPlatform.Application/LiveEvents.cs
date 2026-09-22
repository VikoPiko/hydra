namespace DevOpsPlatform.Application;

public sealed record LiveUpdate(string EventName, object Data);

public interface ILiveEventBus
{
    IAsyncEnumerable<LiveUpdate> SubscribeAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken);
    void Publish(Guid projectId, Guid environmentId, LiveUpdate update);
}
