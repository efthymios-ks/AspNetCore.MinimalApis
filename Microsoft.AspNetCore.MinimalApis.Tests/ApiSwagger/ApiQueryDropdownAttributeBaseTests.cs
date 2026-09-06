using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.OpenApi;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiQueryDropdownAttributeBaseTests
{
    [Fact]
    public void Location_WhenAccessed_ShouldReturnQuery()
    {
        // Arrange
        var attribute = new TestApiQueryDropdownAttribute();

        // Act
        var result = attribute.Location;

        // Assert
        Assert.Equal(ParameterLocation.Query, result);
    }

    private sealed class TestApiQueryDropdownAttribute : ApiQueryDropdownAttributeBase
    {
        public override string Key
            => "test-key";

        public override IEnumerable<string> Values
            => ["a", "b"];
    }
}
