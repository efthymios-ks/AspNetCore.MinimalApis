using Microsoft.AspNetCore.MinimalApis.ApiLogging;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiLogging;

public sealed class EndpointLogScopeOptionsTests
{
    [Fact]
    public void PropertiesSelector_WhenDefault_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var options = new EndpointLogScopeOptions();
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext());

        // Act
        var result = options.PropertiesSelector(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void PropertiesSelector_WhenCustom_ShouldReturnProvidedValues()
    {
        // Arrange
        var options = new EndpointLogScopeOptions
        {
            PropertiesSelector = _ => new Dictionary<string, object?>
            {
                ["key"] = "value"
            }
        };
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext());

        // Act
        var result = options.PropertiesSelector(context);

        // Assert
        Assert.Single(result);
        Assert.Equal("value", result["key"]);
    }
}
