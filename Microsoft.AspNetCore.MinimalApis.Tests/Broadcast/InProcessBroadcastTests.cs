using Microsoft.AspNetCore.MinimalApis.Broadcast;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.Broadcast;

public sealed class InProcessBroadcastTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Stream_WhenBroadcast_ShouldYieldPayload()
    {
        // Arrange
        var transport = new InProcessBroadcast();
        using var cancellation = new CancellationTokenSource();
        await using var stream = transport
            .Stream("topic", cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);
        var move = stream.MoveNextAsync().AsTask();

        // Act
        await transport.Broadcast("topic", "hello");
        var moved = await move.WaitAsync(_timeout);
        var current = stream.Current;

        // Assert
        Assert.True(moved);
        Assert.Equal("hello", current);

        cancellation.Cancel();
    }

    [Fact]
    public async Task Broadcast_WhenMultipleSubscribers_ShouldFanOutToAll()
    {
        // Arrange
        var transport = new InProcessBroadcast();
        using var cancellation = new CancellationTokenSource();
        await using var first = transport
            .Stream("topic", cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);
        await using var second = transport
            .Stream("topic", cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);
        var firstMove = first.MoveNextAsync().AsTask();
        var secondMove = second.MoveNextAsync().AsTask();

        // Act
        await transport.Broadcast("topic", "hello");
        var firstMoved = await firstMove.WaitAsync(_timeout);
        var secondMoved = await secondMove.WaitAsync(_timeout);
        var firstCurrent = first.Current;
        var secondCurrent = second.Current;

        // Assert
        Assert.True(firstMoved);
        Assert.True(secondMoved);
        Assert.Equal("hello", firstCurrent);
        Assert.Equal("hello", secondCurrent);

        cancellation.Cancel();
    }

    [Fact]
    public async Task Broadcast_WhenNoSubscribers_ShouldNotThrow()
    {
        // Arrange
        var transport = new InProcessBroadcast();

        // Act
        var exception = await Record
            .ExceptionAsync(() => transport.Broadcast("topic", "x")
            .AsTask());

        // Assert
        Assert.Null(exception);
    }
}
