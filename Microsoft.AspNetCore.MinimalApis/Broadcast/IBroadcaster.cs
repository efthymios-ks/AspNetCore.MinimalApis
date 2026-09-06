namespace Microsoft.AspNetCore.MinimalApis.Broadcast;

public interface IBroadcaster
{
    IAsyncEnumerable<TMessage> Stream<TMessage>(string channel, CancellationToken cancellationToken);

    ValueTask Broadcast<TMessage>(string channel, TMessage message, CancellationToken cancellationToken = default);
}
