using Microsoft.AspNetCore.MinimalApis.Broadcast;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.Broadcast;

public sealed class RedisBroadcastTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Broadcast_ShouldPublishToPrefixedChannel()
    {
        // Arrange
        var subscriber = Substitute.For<ISubscriber>();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetSubscriber().Returns(subscriber);
        var transport = new RedisBroadcast(multiplexer, "orders:");

        // Act
        await transport.Broadcast("topic", "payload");

        // Assert
        await subscriber.Received(1).PublishAsync(RedisChannel.Literal("orders:topic"), "payload");
    }

    [Fact]
    public async Task Stream_ShouldYieldPayloadPushedThroughSubscription()
    {
        // Arrange
        var subscriber = Substitute.For<ISubscriber>();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetSubscriber().Returns(subscriber);

        Action<RedisChannel, RedisValue> handler = null!;
        subscriber
            .SubscribeAsync(
                Arg.Any<RedisChannel>(),
                Arg.Do<Action<RedisChannel, RedisValue>>(captured => handler = captured),
                Arg.Any<CommandFlags>())
            .Returns(Task.CompletedTask);

        var transport = new RedisBroadcast(multiplexer, keyPrefix: string.Empty);
        using var cancellation = new CancellationTokenSource();
        await using var stream = transport.Stream("topic", cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        var move = stream.MoveNextAsync().AsTask();

        // Act
        handler(RedisChannel.Literal("topic"), "payload");
        var moved = await move.WaitAsync(_timeout);
        var current = stream.Current;

        // Assert
        Assert.NotNull(handler);
        Assert.True(moved);
        Assert.Equal("payload", current);

        cancellation.Cancel();
    }

    [Fact]
    public async Task Stream_WhenMultipleSubscribersSameChannel_ShouldSubscribeOnceAndFanOutToAll()
    {
        // Arrange
        var subscriber = Substitute.For<ISubscriber>();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetSubscriber().Returns(subscriber);

        Action<RedisChannel, RedisValue> handler = null!;
        subscriber
            .SubscribeAsync(
                Arg.Any<RedisChannel>(),
                Arg.Do<Action<RedisChannel, RedisValue>>(captured => handler = captured),
                Arg.Any<CommandFlags>())
            .Returns(Task.CompletedTask);

        var transport = new RedisBroadcast(multiplexer, keyPrefix: string.Empty);
        using var cancellation = new CancellationTokenSource();
        await using var first = transport.Stream("topic", cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await using var second = transport.Stream("topic", cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        var firstMove = first.MoveNextAsync().AsTask();
        var secondMove = second.MoveNextAsync().AsTask();

        // Act
        handler(RedisChannel.Literal("topic"), "payload");
        var firstMoved = await firstMove.WaitAsync(_timeout);
        var secondMoved = await secondMove.WaitAsync(_timeout);
        var firstCurrent = first.Current;
        var secondCurrent = second.Current;

        // Assert
        await subscriber.Received(1).SubscribeAsync(
            Arg.Any<RedisChannel>(),
            Arg.Any<Action<RedisChannel, RedisValue>>(),
            Arg.Any<CommandFlags>());
        Assert.True(firstMoved);
        Assert.True(secondMoved);
        Assert.Equal("payload", firstCurrent);
        Assert.Equal("payload", secondCurrent);

        cancellation.Cancel();
    }

    [Fact]
    public async Task Stream_WhenLastSubscriberLeaves_ShouldUnsubscribeOnce()
    {
        // Arrange
        var subscriber = Substitute.For<ISubscriber>();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetSubscriber().Returns(subscriber);
        subscriber
            .SubscribeAsync(Arg.Any<RedisChannel>(), Arg.Any<Action<RedisChannel, RedisValue>>(), Arg.Any<CommandFlags>())
            .Returns(Task.CompletedTask);

        var transport = new RedisBroadcast(multiplexer, keyPrefix: string.Empty);
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var first = transport.Stream("topic", firstCancellation.Token).GetAsyncEnumerator(firstCancellation.Token);
        var second = transport.Stream("topic", secondCancellation.Token).GetAsyncEnumerator(secondCancellation.Token);
        var firstMove = first.MoveNextAsync().AsTask();
        var secondMove = second.MoveNextAsync().AsTask();

        // Act
        firstCancellation.Cancel();
        await Drain(firstMove);
        await first.DisposeAsync();
        var unsubscribedAfterFirst = subscriber.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(ISubscriber.UnsubscribeAsync));

        secondCancellation.Cancel();
        await Drain(secondMove);
        await second.DisposeAsync();
        var unsubscribedAfterSecond = subscriber.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(ISubscriber.UnsubscribeAsync));

        // Assert
        Assert.Equal(0, unsubscribedAfterFirst);
        Assert.Equal(1, unsubscribedAfterSecond);
    }

    private static async Task Drain(Task move)
    {
        try
        {
            await move;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
