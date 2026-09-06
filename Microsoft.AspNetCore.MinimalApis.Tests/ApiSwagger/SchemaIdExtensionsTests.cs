using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class SchemaIdExtensionsTests
{
    [Fact]
    public void GetSafeSchemaId_WhenNonGeneric_ShouldUseFullName()
    {
        Assert.Equal("System.Int32", typeof(int).GetSafeSchemaId());
        Assert.Equal("System.String", typeof(string).GetSafeSchemaId());
    }

    [Fact]
    public void GetSafeSchemaId_WhenNestedType_ShouldUseDottedFullName()
    {
        var schemaId = typeof(Outer.Inner).GetSafeSchemaId();

        Assert.Equal(typeof(Outer.Inner).FullName!.Replace('+', '.'), schemaId);
        Assert.DoesNotContain('+', schemaId);
    }

    [Fact]
    public void GetSafeSchemaId_WhenGeneric_ShouldComposeArgumentsReadably()
    {
        Assert.Equal(
            "System.Collections.Generic.ListOfSystem.Int32",
            typeof(List<int>).GetSafeSchemaId()
        );

        Assert.Equal(
            "System.Collections.Generic.DictionaryOfSystem.StringAndSystem.Int32",
            typeof(Dictionary<string, int>).GetSafeSchemaId()
        );
    }

    [Fact]
    public void GetSafeSchemaId_WhenNestedGeneric_ShouldComposeRecursively()
    {
        Assert.Equal(
            "System.Collections.Generic.ListOfSystem.Collections.Generic.ListOfSystem.Int32",
            typeof(List<List<int>>).GetSafeSchemaId()
        );
    }

    private sealed class Outer
    {
        public sealed class Inner;
    }
}
