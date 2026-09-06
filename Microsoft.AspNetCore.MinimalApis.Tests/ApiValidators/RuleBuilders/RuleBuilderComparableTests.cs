using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderComparableTests
{
    [Fact]
    public void GreaterThan_WhenValueIsGreater_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.GreaterThan(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 10
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GreaterThan_WhenValueIsEqual_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.GreaterThan(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GreaterThan_WhenValueIsLess_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.GreaterThan(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 3
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GreaterThan_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.GreaterThan(new object());

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GreaterThanOrEqualTo_WhenValueIsGreater_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.GreaterThanOrEqualTo(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 10
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GreaterThanOrEqualTo_WhenValueIsEqual_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.GreaterThanOrEqualTo(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GreaterThanOrEqualTo_WhenValueIsLess_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.GreaterThanOrEqualTo(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 3
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void LessThan_WhenValueIsLess_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.LessThan(10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LessThan_WhenValueIsEqual_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.LessThan(10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 10
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void LessThan_WhenValueIsGreater_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.LessThan(10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 15
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void LessThan_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.LessThan(new object());

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LessThanOrEqualTo_WhenValueIsLess_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.LessThanOrEqualTo(10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LessThanOrEqualTo_WhenValueIsEqual_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.LessThanOrEqualTo(10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 10
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LessThanOrEqualTo_WhenValueIsGreater_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.LessThanOrEqualTo(10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 15
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Equal_WhenValuesAreEqual_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Equal(42);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Equal_WhenValuesAreNotEqual_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.Equal(42);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 99
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NotEqual_WhenValuesAreNotEqual_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NotEqual(42);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 99
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NotEqual_WhenValuesAreEqual_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.NotEqual(42);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void InclusiveBetween_WhenValueInRange_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.InclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void InclusiveBetween_WhenValueIsLowerBound_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.InclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 1
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void InclusiveBetween_WhenValueIsUpperBound_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.InclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 10
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void InclusiveBetween_WhenValueBelowRange_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.InclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 0
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void InclusiveBetween_WhenValueAboveRange_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.InclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 11
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void InclusiveBetween_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.InclusiveBetween(new object(), new object());

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExclusiveBetween_WhenValueInRange_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.ExclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 5
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExclusiveBetween_WhenValueIsLowerBound_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.ExclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 1
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ExclusiveBetween_WhenValueIsUpperBound_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Number);
        builder.ExclusiveBetween(1, 10);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 10
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ExclusiveBetween_WhenPropertyIsNotComparable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildObj(model => model.Obj);
        builder.ExclusiveBetween(new object(), new object());

        // Act
        var result = rules[0](new TestModel
        {
            Obj = new object()
        });

        // Assert
        Assert.Null(result);
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
