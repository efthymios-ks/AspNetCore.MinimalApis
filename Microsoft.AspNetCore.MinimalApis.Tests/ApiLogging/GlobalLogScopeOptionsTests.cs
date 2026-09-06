using Microsoft.AspNetCore.MinimalApis.ApiLogging;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiLogging;

public sealed class GlobalLogScopeOptionsTests
{
    [Fact]
    public void PropertiesSelector_WhenDefault_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var options = new GlobalLogScopeOptions();
        var context = new DefaultHttpContext();

        // Act
        var result = options.PropertiesSelector(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void PropertiesSelector_WhenCustom_ShouldReturnProvidedValues()
    {
        // Arrange
        var options = new GlobalLogScopeOptions
        {
            PropertiesSelector = _ => new Dictionary<string, object?>
            {
                ["key"] = "value"
            }
        };
        var context = new DefaultHttpContext();

        // Act
        var result = options.PropertiesSelector(context);

        // Assert
        Assert.Single(result);
        Assert.Equal("value", result["key"]);
    }
}
