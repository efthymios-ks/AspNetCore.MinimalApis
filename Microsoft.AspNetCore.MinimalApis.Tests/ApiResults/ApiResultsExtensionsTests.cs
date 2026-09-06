using Microsoft.AspNetCore.MinimalApis.ApiResults;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiResults;

public sealed class ApiResultsExtensionsTests
{
    [Fact]
    public void Xml_WhenCalled_ShouldReturnXmlResultWithCorrectValue()
    {
        // Arrange
        var value = new TestModel
        {
            Name = "test"
        };

        // Act
        var result = Results.Extensions.Xml(value);

        // Assert
        var xmlResult = Assert.IsType<XmlResult<TestModel>>(result);
        Assert.Equal(value, xmlResult.Value);
    }

    [Fact]
    public void Xml_WhenDefaultStatusCode_ShouldReturn200()
    {
        // Arrange & Act
        var result = Results.Extensions.Xml(new TestModel
        {
            Name = "x"
        });

        // Assert
        var xmlResult = Assert.IsType<XmlResult<TestModel>>(result);
        Assert.Equal(StatusCodes.Status200OK, xmlResult.StatusCode);
    }

    [Fact]
    public void Xml_WhenStatusCodeProvided_ShouldUseProvidedStatusCode()
    {
        // Arrange & Act
        var result = Results.Extensions.Xml(new TestModel
        {
            Name = "x"
        }, StatusCodes.Status201Created);

        // Assert
        var xmlResult = Assert.IsType<XmlResult<TestModel>>(result);
        Assert.Equal(StatusCodes.Status201Created, xmlResult.StatusCode);
    }

    public sealed class TestModel
    {
        public string? Name { get; set; }
    }
}
