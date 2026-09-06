using Microsoft.AspNetCore.MinimalApis.ApiResults;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using System.Xml.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.Utilities;

public sealed class XmlResultTests
{
    [Fact]
    public void Value_ShouldBeSetFromConstructor()
    {
        // Arrange
        var value = new XmlModel
        {
            Name = "test"
        };

        // Act
        var result = new XmlResult<XmlModel>(value);

        // Assert
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void StatusCode_ShouldDefaultTo200()
    {
        // Arrange & Act
        var result = new XmlResult<XmlModel>(new XmlModel
        {
            Name = "x"
        });

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public void StatusCode_WhenProvided_ShouldUseProvided()
    {
        // Arrange & Act
        var result = new XmlResult<XmlModel>(new XmlModel
        {
            Name = "x"
        }, StatusCodes.Status201Created);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
    }

    [Fact]
    public void ContentType_ShouldBeApplicationXml()
    {
        // Arrange & Act
        var result = new XmlResult<XmlModel>(new XmlModel
        {
            Name = "x"
        });

        // Assert
        Assert.Equal(MediaTypeNames.Application.Xml, result.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetStatusCodeOnResponse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var result = new XmlResult<XmlModel>(new XmlModel
        {
            Name = "x"
        }, StatusCodes.Status201Created);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetContentTypeOnResponse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var result = new XmlResult<XmlModel>(new XmlModel
        {
            Name = "x"
        });

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(MediaTypeNames.Application.Xml, httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteXmlBodyToResponse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var body = new MemoryStream();
        httpContext.Response.Body = body;
        var result = new XmlResult<XmlModel>(new XmlModel
        {
            Name = "Hello"
        });

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        body.Position = 0;
        var written = await new StreamReader(body).ReadToEndAsync();
        Assert.Contains("Hello", written);
    }

    [XmlRoot("XmlModel")]
    public sealed class XmlModel
    {
        public string? Name { get; set; }
    }
}
