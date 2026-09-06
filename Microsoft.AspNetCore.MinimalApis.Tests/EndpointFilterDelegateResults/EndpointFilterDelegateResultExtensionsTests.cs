using Microsoft.AspNetCore.MinimalApis.EndpointFilterDelegateResults;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.EndpointFilterDelegateResults;

public sealed class EndpointFilterDelegateResultExtensionsTests
{
    [Fact]
    public async Task RunAsync_WhenNextReturnsNull_ShouldReturnNull()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(null);

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_WhenResultHasStatusCode_ShouldExtractStatusCode()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(Results.Created("/resource", "value"));

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
    }

    [Fact]
    public async Task RunAsync_WhenResultHasNoStatusCode_ShouldDefault200()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());
        var rawResult = new NoStatusCodeResult();

        ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(rawResult);

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public async Task RunAsync_WhenResultHasValue_ShouldExtractValue()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(Results.Ok("extracted-value"));

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("extracted-value", result.Value);
    }

    [Fact]
    public async Task RunAsync_WhenResultHasNoValue_ShouldUseResultAsValue()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());
        var rawResult = new NoValueResult();

        ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(rawResult);

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Same(rawResult, result.Value);
    }

    [Fact]
    public async Task RunAsync_WhenResultHasContentType_ShouldExtractContentType()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(Results.Content("<xml/>", MediaTypeNames.Application.Xml));

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MediaTypeNames.Application.Xml, result.ContentType);
    }

    [Fact]
    public async Task RunAsync_WhenResultHasNoContentType_ShouldDefaultToJson()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());
        var rawResult = new NoContentTypeResult();

        ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(rawResult);

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MediaTypeNames.Application.Json, result.ContentType);
    }

    [Fact]
    public async Task RunAsync_WhenResultIsNested_ShouldUnwrapToInner()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());
        var inner = Results.Ok("inner-value");
        var nested = new TestNestedResult(inner);

        ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(nested);

        // Act
        var result = await context.RunAsync(Next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("inner-value", result.Value);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public async Task RunAsync_WhenResultIsMultipleNested_ShouldUnwrapFully()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());
        var deepest = Results.Ok("deep-value");
        var middle = new TestNestedResult(deepest);
        var outer = new TestNestedResult(middle);

        ValueTask<object?> next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(outer);

        // Act
        var result = await context.RunAsync(next);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("deep-value", result.Value);
    }

    [Fact]
    public async Task RunAsync_WhenResultIsNotAnIResult_ShouldThrow()
    {
        // Arrange
        var context = new TestEndpointFilterInvocationContext(new DefaultHttpContext());

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>("not an IResult");

        // Act
        var exception = await Record.ExceptionAsync(() => context.RunAsync(Next));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;
        public override IList<object?> Arguments { get; } = [];

        public override TArgument GetArgument<TArgument>(int index)
            => (TArgument)Arguments[index]!;
    }

    private sealed class NoStatusCodeResult : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => Task.CompletedTask;
    }

    private sealed class NoValueResult : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => Task.CompletedTask;
    }

    private sealed class NoContentTypeResult : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => Task.CompletedTask;
    }

    private sealed class TestNestedResult(IResult inner) : INestedHttpResult, IResult
    {
        public IResult Result { get; } = inner;

        public Task ExecuteAsync(HttpContext httpContext)
            => Result.ExecuteAsync(httpContext);
    }
}
