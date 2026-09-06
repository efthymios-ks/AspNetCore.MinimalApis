using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderEnumTests
{
    [Fact]
    public void IsInEnum_WhenValidEnumValue_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildEnum(model => model.Status);
        builder.IsInEnum();

        // Act
        var result = rules[0](new TestModel
        {
            Status = TestStatus.Active
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsInEnum_WhenInvalidEnumValue_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = BuildEnum(model => model.Status);
        builder.IsInEnum();

        // Act
        var result = rules[0](new TestModel
        {
            Status = (TestStatus)999
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void IsInEnum_WhenPropertyIsNotEnum_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.IsInEnum();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsInEnum_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = BuildEnum(model => model.Status);
        builder.IsInEnum().WithMessage("invalid status");

        // Act
        var result = rules[0](new TestModel
        {
            Status = (TestStatus)999
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invalid status", result.ErrorMessage);
    }

    [Fact]
    public void IsInEnum_Generic_WhenValidValue_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.RawValue);
        builder.IsInEnum<TestStatus>();

        // Act
        var result = rules[0](new TestModel { RawValue = TestStatus.Active });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsInEnum_Generic_WhenInvalidValue_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.RawValue);
        builder.IsInEnum<TestStatus>();

        // Act
        var result = rules[0](new TestModel
        {
            RawValue = 999
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void IsInEnum_Generic_WhenValueIsNull_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.RawValue);
        builder.IsInEnum<TestStatus>();

        // Act
        var result = rules[0](new TestModel
        {
            RawValue = null
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsInEnum_Generic_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.RawValue);
        builder.IsInEnum<TestStatus>().WithMessage("bad enum");

        // Act
        var result = rules[0](new TestModel
        {
            RawValue = 999
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bad enum", result.ErrorMessage);
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, TestStatus>) BuildEnum(
        Expression<Func<TestModel, TestStatus>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, TestStatus>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, int>) BuildInt(
        Expression<Func<TestModel, int>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, int>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, object?>) BuildObj(
        Expression<Func<TestModel, object?>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, object?>(selector, rules));
    }

    public enum TestStatus
    {
        Active = 1,
        Inactive = 2
    }

    public sealed class TestModel
    {
        public TestStatus Status { get; set; }
        public int Number { get; set; }
        public object? RawValue { get; set; }
    }
}
