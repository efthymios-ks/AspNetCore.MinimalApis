using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderBusinessTests
{
    [Fact]
    public void EmailAddress_WhenValidEmail_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EmailAddress();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "user@example.com"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EmailAddress_WhenInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EmailAddress();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "not-an-email"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void EmailAddress_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.EmailAddress();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EmailAddress_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.EmailAddress().WithMessage("bad email");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "not-an-email"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bad email", result.ErrorMessage);
    }

    [Fact]
    public void AbsoluteUrl_WhenValidAbsoluteUrl_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.AbsoluteUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "https://example.com/path"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AbsoluteUrl_WhenRelativeUrl_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.AbsoluteUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "/relative/path"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void AbsoluteUrl_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.AbsoluteUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AbsoluteUrl_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.AbsoluteUrl().WithMessage("bad url");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "not-a-url"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bad url", result.ErrorMessage);
    }

    [Fact]
    public void RelativeUrl_WhenValidRelativeUrl_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.RelativeUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "/relative/path"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RelativeUrl_WhenDotRelativeUrl_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.RelativeUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "./relative"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RelativeUrl_WhenAbsoluteUrl_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.RelativeUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "https://example.com"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void RelativeUrl_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.RelativeUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RelativeUrl_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.RelativeUrl().WithMessage("bad relative url");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "not-a-relative-url"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bad relative url", result.ErrorMessage);
    }

    [Fact]
    public void CreditCard_WhenValidNumber_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.CreditCard();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "4111111111111111"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreditCard_WhenInvalidNumber_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.CreditCard();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "1234567890"
        });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void CreditCard_WhenPropertyIsNotString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = BuildInt(model => model.Number);
        builder.CreditCard();

        // Act
        var result = rules[0](new TestModel
        {
            Number = 42
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreditCard_WhenCustomMessage_ShouldUseIt()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.CreditCard().WithMessage("bad card");

        // Act
        var result = rules[0](new TestModel
        {
            Value = "invalid"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bad card", result.ErrorMessage);
    }

    [Fact]
    public void RelativeUrl_WhenUrlHasQueryString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.RelativeUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "/relative/path?foo=bar"
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AbsoluteUrl_WhenUrlHasQueryString_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Value);
        builder.AbsoluteUrl();

        // Act
        var result = rules[0](new TestModel
        {
            Value = "https://example.com/path?foo=bar"
        });

        // Assert
        Assert.Null(result);
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
