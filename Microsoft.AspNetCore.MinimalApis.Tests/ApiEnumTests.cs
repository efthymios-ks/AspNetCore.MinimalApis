using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests;

public sealed class ApiEnumTests
{
    [Theory]
    [InlineData("First", SampleEnum.First)]
    [InlineData("Second", SampleEnum.Second)]
    [InlineData("second", SampleEnum.Second)]
    [InlineData("SECOND", SampleEnum.Second)]
    [InlineData("0", SampleEnum.First)]
    [InlineData("1", SampleEnum.Second)]
    public void TryParse_WhenMemberNameAnyCaseOrNumericValue_ShouldSucceed(string input, SampleEnum expected)
    {
        // Act
        var parsed = ApiEnum<SampleEnum>.TryParse(input, provider: null, out var result);

        // Assert
        Assert.True(parsed);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void TryParse_WhenNumericOutOfRange_ShouldSucceedWithUndefinedValue()
    {
        // Act
        var parsed = ApiEnum<SampleEnum>.TryParse("99", provider: null, out var result);

        // Assert
        Assert.True(parsed);
        Assert.Equal((SampleEnum)99, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("banana")]
    public void TryParse_WhenInvalid_ShouldFailAndReturnDefault(string? input)
    {
        // Act
        var parsed = ApiEnum<SampleEnum>.TryParse(input, provider: null, out var result);

        // Assert
        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    [Fact]
    public void Parse_WhenMemberNameDifferentCase_ShouldReturnValue()
    {
        // Act
        var result = ApiEnum<SampleEnum>.Parse("second", provider: null);

        // Assert
        Assert.Equal(SampleEnum.Second, result.Value);
    }

    [Fact]
    public void Parse_WhenInvalid_ShouldThrowFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => ApiEnum<SampleEnum>.Parse("banana", provider: null));
    }

    [Fact]
    public void ImplicitConversion_WhenAssignedToEnum_ShouldUnwrapToEnum()
    {
        // Arrange
        var wrapped = new ApiEnum<SampleEnum>(SampleEnum.Second);

        // Act
        SampleEnum value = wrapped;

        // Assert
        Assert.Equal(SampleEnum.Second, value);
    }

    [Fact]
    public void ImplicitConversion_WhenAssignedFromEnum_ShouldWrapEnum()
    {
        // Act
        ApiEnum<SampleEnum> wrapped = SampleEnum.Second;

        // Assert
        Assert.Equal(SampleEnum.Second, wrapped.Value);
    }

    [Fact]
    public void ToString_WhenCalled_ShouldReturnEnumMemberName()
    {
        // Arrange
        var wrapped = new ApiEnum<SampleEnum>(SampleEnum.Second);

        // Act & Assert
        Assert.Equal("Second", wrapped.ToString());
    }
}

public enum SampleEnum
{
    First,
    Second,
}
