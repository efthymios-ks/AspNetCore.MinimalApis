using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderCollectionTests
{
    [Fact]
    public void NotEmpty_WhenCollectionHasItems_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.NotEmpty();

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a"]
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NotEmpty_WhenCollectionIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.NotEmpty();

        // Act
        var result = rules[0](new TestModel
        {
            Items = []
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NotEmpty_WhenPropertyIsNotEnumerable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.NotEmpty();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MinimumLength_WhenLengthSufficient_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.MinimumLength(2);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b", "c"]
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MinimumLength_WhenLengthInsufficient_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.MinimumLength(5);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a"]
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void MinimumLength_WhenPropertyIsNotEnumerable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.MinimumLength(1);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MinimumLength_WhenPropertyIsString_ShouldUseStringLength()
    {
        // Arrange
        var (rules, builder) = BuildStr(model => model.Name);
        builder.MinimumLength(3);

        // Act
        var pass = rules[0](new TestModel
        {
            Name = "abcd"
        });

        var fail = rules[0](new TestModel
        {
            Name = "ab"
        });

        // Assert
        Assert.Null(pass);
        Assert.NotNull(fail);
    }

    [Fact]
    public void MinimumLength_WhenPropertyIsICollection_ShouldUseCount()
    {
        // Arrange
        var (rules, builder) = BuildList(model => model.List);
        builder.MinimumLength(2);

        // Act
        var pass = rules[0](new TestModel
        {
            List = ["a", "b"]
        });

        var fail = rules[0](new TestModel
        {
            List = ["a"]
        });

        // Assert
        Assert.Null(pass);
        Assert.NotNull(fail);
    }

    [Fact]
    public void MinimumLength_WhenPropertyIsPlainEnumerable_ShouldCountSlow()
    {
        // Arrange
        var (rules, builder) = BuildEnumerable(model => model.Enumerable);
        builder.MinimumLength(2);

        // Act
        var pass = rules[0](new TestModel
        {
            Enumerable = new SlowEnumerable(3)
        });

        var fail = rules[0](new TestModel
        {
            Enumerable = new SlowEnumerable(1)
        });

        // Assert
        Assert.Null(pass);
        Assert.NotNull(fail);
    }

    [Fact]
    public void MaximumLength_WhenLengthWithinLimit_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.MaximumLength(5);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b"]
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MaximumLength_WhenLengthExceedsLimit_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.MaximumLength(2);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b", "c"]
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void MaximumLength_WhenPropertyIsNotEnumerable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.MaximumLength(5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Length_Exact_WhenExactLength_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Length(3);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b", "c"]
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Length_Exact_WhenWrongLength_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Length(3);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b"]
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Length_Exact_WhenPropertyIsNotEnumerable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.Length(3);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Length_Range_WhenLengthInRange_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Length(2, 5);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b", "c"]
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Length_Range_WhenLengthBelowRange_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Length(3, 5);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a"]
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Length_Range_WhenLengthAboveRange_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Length(1, 2);

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b", "c"]
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Length_Range_WhenPropertyIsNotEnumerable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.Length(1, 5);

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ForEach_WhenAllElementsValid_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.ForEach<string>(element => element.RuleFor(value => value).NotEmpty());

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", "b"]
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ForEach_WhenElementInvalid_ShouldReturnFailureWithIndexedMemberName()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.ForEach<string>(element => element.RuleFor(value => value).NotEmpty());

        // Act
        var result = rules[0](new TestModel
        {
            Items = ["a", ""]
        });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Items[1]", result.MemberNames);
    }

    [Fact]
    public void ForEach_WhenPropertyIsNotEnumerable_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.ForEach<string>(element => element.RuleFor(value => value).NotEmpty());

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ForEach_WhenActionIsNull_ShouldThrow()
    {
        // Arrange
        var (_, builder) = Build(model => model.Items);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ForEach<string>(null!));
    }

    [Fact]
    public void Empty_WhenCollectionHasItems_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Empty();

        // Act
        var result = rules[0](new TestModel { Items = ["a"] });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Empty_WhenCollectionIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Items);
        builder.Empty();

        // Act
        var result = rules[0](new TestModel { Items = [] });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Empty_WhenStringIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildStr(model => model.Name);
        builder.Empty();

        // Act
        var result = rules[0](new TestModel { Name = "" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NotEmpty_WhenStringIsNull_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = BuildStr(model => model.Name);
        builder.NotEmpty();

        // Act
        var result = rules[0](new TestModel { Name = null });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NotEmpty_WhenStringIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = BuildStr(model => model.Name);
        builder.NotEmpty();

        // Act
        var result = rules[0](new TestModel
        {
            Name = ""
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NotEmpty_WhenStringIsNotEmpty_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildStr(model => model.Name);
        builder.NotEmpty();

        // Act
        var result = rules[0](new TestModel
        {
            Name = "value"
        });

        // Assert
        Assert.Null(result);
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, string[]>) Build(
        Expression<Func<TestModel, string[]>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, string[]>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, string?>) BuildStr(
        Expression<Func<TestModel, string?>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, string?>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, List<string>>) BuildList(
        Expression<Func<TestModel, List<string>>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, List<string>>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, IEnumerable>) BuildEnumerable(
        Expression<Func<TestModel, IEnumerable>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, IEnumerable>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, int>) BuildInt(
        Expression<Func<TestModel, int>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, int>(selector, rules));
    }

    public sealed class TestModel
    {
        public string[] Items { get; set; } = [];
        public string? Name { get; set; }
        public List<string> List { get; set; } = [];
        public IEnumerable Enumerable { get; set; } = Array.Empty<object>();
        public int Number { get; set; }
    }

    private sealed class SlowEnumerable(int count) : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            for (var i = 0; i < count; i++)
            {
                yield return i;
            }
        }
    }
}
