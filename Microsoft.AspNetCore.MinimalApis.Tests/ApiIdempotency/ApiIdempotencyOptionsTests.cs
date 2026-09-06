using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiIdempotency;

public sealed class ApiIdempotencyOptionsTests
{
    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludeMethod()
    {
        // Arrange
        var context = BuildContext("POST", "/api/orders", "");

        // Act
        var key = ApiIdempotencyOptions.GetKeyPrefix(context);

        // Assert
        Assert.Contains("POST", key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludePath()
    {
        // Arrange
        var context = BuildContext("POST", "/api/orders", "");

        // Act
        var key = ApiIdempotencyOptions.GetKeyPrefix(context);

        // Assert
        Assert.Contains("/api/orders", key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludeQueryString()
    {
        // Arrange
        var context = BuildContext("POST", "/api/orders", "?ref=abc");

        // Act
        var key = ApiIdempotencyOptions.GetKeyPrefix(context);

        // Assert
        Assert.Contains("ref=abc", key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldStartWithApiIdempotencyPrefix()
    {
        // Arrange
        var context = BuildContext("POST", "/", "");

        // Act
        var key = ApiIdempotencyOptions.GetKeyPrefix(context);

        // Assert
        Assert.StartsWith("ApiIdempotency:", key);
    }

    [Fact]
    public void CacheDuration_ShouldDefaultToThirtyMinutes()
    {
        var options = new ApiIdempotencyOptions();

        Assert.Equal(TimeSpan.FromMinutes(30), options.CacheDuration);
    }

    [Fact]
    public void ProcessingTimeout_ShouldDefaultToThirtySeconds()
    {
        var options = new ApiIdempotencyOptions();

        Assert.Equal(TimeSpan.FromSeconds(30), options.ProcessingTimeout);
    }

    [Fact]
    public void KeySuffixFactory_WhenDefault_ShouldReturnEmptyString()
    {
        // Arrange
        var options = new ApiIdempotencyOptions();
        var context = BuildContext("POST", "/", "");

        // Act
        var suffix = options.KeySuffixFactory(context);

        // Assert
        Assert.Equal(string.Empty, suffix);
    }

    private static TestContext BuildContext(
        string method, string path, string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = new PathString(path);
        if (queryString.Length > 0)
        {
            httpContext.Request.QueryString = new QueryString(queryString);
        }

        return new(httpContext);
    }

    private sealed class TestContext(HttpContext httpContext) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;
        public override IList<object?> Arguments { get; } = [];

        public override TArgument GetArgument<TArgument>(int index)
            => (TArgument)Arguments[index]!;
    }
}
