using Microsoft.AspNetCore.MinimalApis.Utilities;
using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.Utilities;

public sealed class ContentUtilsTests
{
    [Fact]
    public async Task ToBytesAsync_WhenValueIsNull_ShouldReturnEmptyArray()
    {
        // Arrange
        object? value = null;

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Application.Json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ToBytesAsync_WhenValueIsStream_ShouldReturnStreamBytes()
    {
        // Arrange
        var expected = "stream content"u8.ToArray();
        var stream = new MemoryStream(expected);

        // Act
        var result = await stream.ToBytesAsync(MediaTypeNames.Application.Json);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ToBytesAsync_WhenValueIsByteArray_ShouldReturnSameBytes()
    {
        // Arrange
        var expected = new byte[] { 1, 2, 3, 4 };

        // Act
        var result = await expected.ToBytesAsync(MediaTypeNames.Application.Json);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ToBytesAsync_WhenValueIsString_ShouldReturnUtf8Bytes()
    {
        // Arrange
        const string value = "hello world";

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Application.Json);

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes(value), result);
    }

    [Fact]
    public async Task ToBytesAsync_WhenContentTypeIsApplicationXml_ShouldReturnXmlBytes()
    {
        // Arrange
        var value = new XmlSerializable
        {
            Name = "test"
        };

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Application.Xml);

        // Assert
        Assert.NotEmpty(result);
        var xml = Encoding.UTF8.GetString(result);
        Assert.Contains("test", xml);
    }

    [Fact]
    public async Task ToBytesAsync_WhenContentTypeIsApplicationXmlPatch_ShouldReturnXmlBytes()
    {
        // Arrange
        var value = new XmlSerializable
        {
            Name = "patch"
        };

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Application.XmlPatch);

        // Assert
        Assert.NotEmpty(result);
        var xml = Encoding.UTF8.GetString(result);
        Assert.Contains("patch", xml);
    }

    [Fact]
    public async Task ToBytesAsync_WhenContentTypeIsApplicationProblemXml_ShouldReturnXmlBytes()
    {
        // Arrange
        var value = new XmlSerializable
        {
            Name = "problem"
        };

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Application.ProblemXml);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ToBytesAsync_WhenContentTypeIsTextXml_ShouldReturnXmlBytes()
    {
        // Arrange
        var value = new XmlSerializable
        {
            Name = "text"
        };

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Text.Xml);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ToBytesAsync_WhenContentTypeIsJson_ShouldReturnJsonBytes()
    {
        // Arrange
        var value = new
        {
            Name = "json-value"
        };

        // Act
        var result = await value.ToBytesAsync(MediaTypeNames.Application.Json);

        // Assert
        var json = Encoding.UTF8.GetString(result);
        Assert.Contains("json-value", json);
    }

    [Fact]
    public async Task ToBytesAsync_WhenContentTypeIsNull_ShouldReturnJsonBytes()
    {
        // Arrange
        var value = new
        {
            Name = "null-content-type"
        };

        // Act
        var result = await value.ToBytesAsync(null);

        // Assert
        var json = Encoding.UTF8.GetString(result);
        Assert.Contains("null-content-type", json);
    }

    [XmlRoot("XmlSerializable")]
    public sealed class XmlSerializable
    {
        public string? Name { get; set; }
    }
}
