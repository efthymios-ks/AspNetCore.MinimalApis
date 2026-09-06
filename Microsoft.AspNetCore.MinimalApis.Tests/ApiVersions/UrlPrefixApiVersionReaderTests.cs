using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiVersions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiVersions;

public sealed class UrlPrefixApiVersionReaderTests
{
    private readonly UrlPrefixApiVersionReader _reader = new();

    [Fact]
    public void AddParameters_WhenCalled_ShouldDoNothing()
    {
        // Arrange
        var context = Substitute.For<IApiVersionParameterDescriptionContext>();

        // Act
        _reader.AddParameters(context);

        // Assert
        context.ReceivedWithAnyArgs(0).AddParameter(default!, default);
    }

    [Fact]
    public void Read_WhenPathIsNull_ShouldReturnEmpty()
    {
        // Arrange
        var request = BuildRequest(null);

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Read_WhenPathIsRoot_ShouldReturnEmpty()
    {
        // Arrange
        var request = BuildRequest("/");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Read_WhenPathHasVersionSegment_ShouldReturnVersionNumber()
    {
        // Arrange
        var request = BuildRequest("/v2/users");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("2", result[0]);
    }

    [Fact]
    public void Read_WhenPathHasNoVersionSegment_ShouldReturnEmpty()
    {
        // Arrange
        var request = BuildRequest("/api/users");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Read_WhenVersionIsNotInteger_ShouldReturnEmpty()
    {
        // Arrange
        var request = BuildRequest("/vabc/users");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Read_WhenVersionIsUppercase_ShouldReturnVersionNumber()
    {
        // Arrange
        var request = BuildRequest("/V3/orders");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("3", result[0]);
    }

    [Fact]
    public void Read_WhenPathHasVersionInMiddle_ShouldReturnVersionNumber()
    {
        // Arrange
        var request = BuildRequest("/api/v1/users");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result[0]);
    }

    [Fact]
    public void Read_WhenPathHasMultipleVersionSegments_ShouldReturnFirst()
    {
        // Arrange
        var request = BuildRequest("/v1/api/v2/users");

        // Act
        var result = _reader.Read(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result[0]);
    }

    private static HttpRequest BuildRequest(string? path)
    {
        var context = new DefaultHttpContext();
        if (path is not null)
        {
            context.Request.Path = new PathString(path);
        }

        return context.Request;
    }
}
