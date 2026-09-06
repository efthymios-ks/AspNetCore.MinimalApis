using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.OpenApi;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiParameterBaseTests
{
    private readonly TestApiParameter _parameter = new();

    [Fact]
    public void DefaultValue_WhenNotOverridden_ShouldReturnNull()
    {
        // Act
        var result = _parameter.DefaultValue;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsRequired_WhenNotOverridden_ShouldReturnFalse()
    {
        // Act
        var result = _parameter.IsRequired;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ConfigureAsync_WhenNotOverridden_ShouldReturnCompletedTask()
    {
        // Act
        var exception = await Record.ExceptionAsync(() => _parameter.ConfigureAsync(null!, null!, null!));

        // Assert
        Assert.Null(exception);
    }

    private sealed class TestApiParameter : ApiParameterBase
    {
        public override ParameterLocation Location { get; } = ParameterLocation.Query;

        public override string Key { get; } = "test-key";
    }
}
