namespace Microsoft.AspNetCore.MinimalApis.Broadcast;

public interface IBroadcastTransport
{
    IAsyncEnumerable<string> Stream(
        string channel,
        CancellationToken cancellationToken
    );

    ValueTask Broadcast(
        string channel,
        string payload,
        CancellationToken cancellationToken = default
    );
}
