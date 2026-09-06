using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderStringTests
{
    [Fact]
    public void Matches_WhenPatternMatches_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Matches(@"^\d+$");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "12345"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Matches_WhenPatternDoesNotMatch_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Matches(@"^\d+$");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "abc"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Matches_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.Matches(@"^\d+$");

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Matches_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Matches(@"^\d+$").WithMessage("digits only");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "abc"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("digits only", result.ErrorMessage);
    }

    [Fact]
    public void Contains_WhenValueContainsSubstring_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Contains("hello");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "say hello world"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Contains_WhenValueMissingSubstring_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Contains("hello");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "goodbye world"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Contains_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.Contains("1");

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Contains_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Contains("hello").WithMessage("must contain hello");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "bye"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("must contain hello", result.ErrorMessage);
    }

    [Fact]
    public void StartsWith_WhenValueStartsWithPrefix_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.StartsWith("hello");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "hello world"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void StartsWith_WhenValueDoesNotStartWithPrefix_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.StartsWith("hello");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "world hello"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void StartsWith_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.StartsWith("4");

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void StartsWith_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.StartsWith("hello").WithMessage("must start with hello");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "world"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("must start with hello", result.ErrorMessage);
    }

    [Fact]
    public void EndsWith_WhenValueEndsWithSuffix_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EndsWith("world");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "hello world"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EndsWith_WhenValueDoesNotEndWithSuffix_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EndsWith("world");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "world hello"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void EndsWith_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.EndsWith("2");

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EndsWith_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EndsWith("world").WithMessage("must end with world");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "hello"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("must end with world", result.ErrorMessage);
    }

    [Fact]
    public void Contains_WhenCaseSensitiveAndDifferentCase_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.Contains("Hello", StringComparison.Ordinal);

        // Act
        var result = rules[0](new TestModel
        {
            Value = "say hello world"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void StartsWith_WhenCaseSensitiveAndDifferentCase_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.StartsWith("Hello", StringComparison.Ordinal);

        // Act
        var result = rules[0](new TestModel
        {
            Value = "hello world"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void EndsWith_WhenCaseSensitiveAndDifferentCase_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EndsWith("World", StringComparison.Ordinal);

        // Act
        var result = rules[0](new TestModel
        {
            Value = "hello world"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Matches_WhenPatternIsNull_ShouldThrow()
    {
        // Arrange
        var (_, builder) = Build(model => model.Value);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.Matches(null!));
    }

    [Fact]
    public void Contains_WhenValueIsWhitespace_ShouldThrow()
    {
        // Arrange
        var (_, builder) = Build(model => model.Value);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.Contains(" "));
    }

    [Fact]
    public void StartsWith_WhenValueIsWhitespace_ShouldThrow()
    {
        // Arrange
        var (_, builder) = Build(model => model.Value);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.StartsWith(" "));
    }

    [Fact]
    public void EndsWith_WhenValueIsWhitespace_ShouldThrow()
    {
        // Arrange
        var (_, builder) = Build(model => model.Value);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.EndsWith(" "));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, string?>) Build(
        Expression<Func<TestModel, string?>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, string?>(selector, rules));
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, int>) BuildInt(
        Expression<Func<TestModel, int>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, int>(selector, rules));
    }

    public sealed class TestModel
    {
        public string? Value { get; set; }
        public int Number { get; set; }
    }
}
