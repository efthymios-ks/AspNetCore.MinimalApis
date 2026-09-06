using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderNumericTests
{
    [Fact]
    public void Zero_WhenValueIsZero_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Zero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Zero_WhenValueIsNotZero_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Zero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Zero_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.Zero();

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NotZero_WhenValueIsNotZero_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NotZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NotZero_WhenValueIsZero_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NotZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NotZero_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.NotZero();

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Positive_WhenValueIsPositive_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Positive();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 1
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Positive_WhenValueIsZero_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Positive();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Positive_WhenValueIsNegative_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Positive();

        // Act
        var result = rules[0](new TestModel
        {
            Number = -1
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Positive_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.Positive();

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PositiveOrZero_WhenValueIsPositive_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.PositiveOrZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PositiveOrZero_WhenValueIsZero_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.PositiveOrZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PositiveOrZero_WhenValueIsNegative_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.PositiveOrZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = -1
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Negative_WhenValueIsNegative_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Negative();

        // Act
        var result = rules[0](new TestModel
        {
            Number = -1
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Negative_WhenValueIsZero_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Negative();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Negative_WhenValueIsPositive_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Negative();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 1
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Negative_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.Negative();

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NegativeOrZero_WhenValueIsNegative_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NegativeOrZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = -5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NegativeOrZero_WhenValueIsZero_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NegativeOrZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NegativeOrZero_WhenValueIsPositive_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NegativeOrZero();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 1
        });

        // Assert
        Assert.NotNull(result);
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, int>) Build(
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

    public sealed class TestModel
    {
        public int Number { get; set; }
        public object? Obj { get; set; }
    }
}
