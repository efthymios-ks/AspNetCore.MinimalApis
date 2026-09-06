using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiIdempotency;

public sealed class ApiIdempotencyBehaviorTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task WhenSameKeySentTwice_ShouldReplayFirstResponseAndRunHandlerOnce()
    {
        // Arrange
        var handlerCalls = 0;
        await using var app = BuildApp(() =>
        {
            var call = Interlocked.Increment(ref handlerCalls);
            return Results.Ok(call);
        });
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var first = await PostAsync(client, "key-1");
        var second = await PostAsync(client, "key-1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(1, handlerCalls);
        Assert.Equal(
            await first.Content.ReadAsStringAsync(),
            await second.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task WhenDifferentKeys_ShouldRunHandlerForEach()
    {
        // Arrange
        var handlerCalls = 0;
        await using var app = BuildApp(() =>
        {
            Interlocked.Increment(ref handlerCalls);
            return Results.Ok("value");
        });
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        await PostAsync(client, "key-1");
        await PostAsync(client, "key-2");

        // Assert
        Assert.Equal(2, handlerCalls);
    }

    [Fact]
    public async Task WhenFirstResponseIsNonSuccess_ShouldNotCacheAndAllowRetry()
    {
        // Arrange
        var handlerCalls = 0;
        await using var app = BuildApp(() =>
        {
            var call = Interlocked.Increment(ref handlerCalls);
            return call == 1
                ? Results.StatusCode(StatusCodes.Status500InternalServerError)
                : Results.Ok("recovered");
        });
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var first = await PostAsync(client, "key-1");
        var second = await PostAsync(client, "key-1");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(2, handlerCalls);
        Assert.Equal("recovered", (await second.Content.ReadAsStringAsync()).Trim('"'));
    }

    [Fact]
    public async Task WhenDuplicateArrivesWhileFirstInFlight_ShouldReturn409()
    {
        // Arrange
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = BuildApp(async () =>
        {
            entered.TrySetResult();
            await release.Task;
            return Results.Ok("done");
        });
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var firstTask = PostAsync(client, "key-1");
        await entered.Task.WaitAsync(_timeout);
        var second = await PostAsync(client, "key-1").WaitAsync(_timeout);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        release.SetResult();
        var first = await firstTask.WaitAsync(_timeout);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
    }

    [Fact]
    public async Task WhenIdempotencyKeyMissing_ShouldReturn400()
    {
        // Arrange
        await using var app = BuildApp(() => Results.Ok("value"));
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.PostAsync("/orders", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenReplayingObjectResponse_ShouldSerializeBodyWithHostJsonOptions()
    {
        // Arrange
        await using var app = BuildApp(
            () => Results.Ok(new CasingSample(42)),
            services => services.ConfigureHttpJsonOptions(options
                => options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase));
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act — first runs the handler, second replays the stored response
        var first = await PostAsync(client, "key-1");
        var second = await PostAsync(client, "key-1");

        // Assert — the replayed body honours the host camelCase options, exactly like the live one
        Assert.Contains("\"sampleValue\":42", await first.Content.ReadAsStringAsync());
        var replayed = await second.Content.ReadAsStringAsync();
        Assert.Contains("\"sampleValue\":42", replayed);
        Assert.DoesNotContain("SampleValue", replayed);
    }

    private static WebApplication BuildApp(Delegate handler, Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddApiIdempotency(options =>
            options.KeySuffixFactory = context
                => context.HttpContext.Request.Headers["Idempotency-Key"].ToString()
        );
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        app.MapPost("/orders", handler).WithApiIdempotency();
        return app;
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private sealed record CasingSample(int SampleValue);
}
