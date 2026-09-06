using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderCoreTests
{
    [Fact]
    public void NotNull_WhenPropertyIsNull_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull();

        // Act
        var result = rules[0](new TestModel { Name = null });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Name", result.MemberNames);
    }

    [Fact]
    public void NotNull_WhenPropertyIsNotNull_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull();

        // Act
        var result = rules[0](new TestModel { Name = "value" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Null_WhenPropertyIsNotNull_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.Null();

        // Act
        var result = rules[0](new TestModel { Name = "value" });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Name", result.MemberNames);
    }

    [Fact]
    public void Null_WhenPropertyIsNull_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.Null();

        // Act
        var result = rules[0](new TestModel { Name = null });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void WithMessage_WhenRuleFails_ShouldOverrideMessage()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull().WithMessage("custom message");

        // Act
        var result = rules[0](new TestModel { Name = null });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("custom message", result.ErrorMessage);
    }

    [Fact]
    public void WithName_WhenRuleFails_ShouldOverrideMemberName()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull().WithName("displayName");

        // Act
        var result = rules[0](new TestModel { Name = null });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("displayName", result.MemberNames);
        Assert.DoesNotContain("Name", result.MemberNames);
    }

    [Fact]
    public void WithErrorCode_WhenRuleFails_ShouldProduceApiValidationResultWithCode()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull().WithErrorCode("REQUIRED");

        // Act
        var result = rules[0](new TestModel { Name = null });

        // Assert
        var apiResult = Assert.IsType<ApiValidationResult>(result);
        Assert.Equal("REQUIRED", apiResult.ErrorCode);
        Assert.Contains("Name", apiResult.MemberNames);
    }

    [Fact]
    public void When_WhenConditionIsFalse_ShouldSkipRule()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull().When(model => model.Tag == "check");

        // Act
        var result = rules[0](new TestModel { Name = null, Tag = "skip" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void When_WhenConditionIsTrue_ShouldApplyRule()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull().When(model => model.Tag == "check");

        // Act
        var result = rules[0](new TestModel { Name = null, Tag = "check" });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Unless_WhenConditionIsTrue_ShouldSkipRule()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.NotNull().Unless(model => model.Tag == "skip");

        // Act
        var result = rules[0](new TestModel { Name = null, Tag = "skip" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void When_WhenAppliedToChain_ShouldGuardAllPrecedingRules()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder
            .NotNull()
            .NotEmpty()
            .When(model => model.Tag == "check");

        // Act
        var firstResult = rules[0](new TestModel { Name = null, Tag = "skip" });
        var secondResult = rules[1](new TestModel { Name = null, Tag = "skip" });

        // Assert
        Assert.Null(firstResult);
        Assert.Null(secondResult);
    }

    [Fact]
    public void Must_WhenPredicateFails_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.Must(value => value is not null && value.Length > 3).WithMessage("too short");

        // Act
        var result = rules[0](new TestModel { Name = "ab" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("too short", result.ErrorMessage);
    }

    [Fact]
    public void Must_WhenPredicatePasses_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.Must(value => value is not null && value.Length > 3);

        // Act
        var result = rules[0](new TestModel { Name = "valid" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Must_WithInstance_WhenPredicateFails_ShouldReturnFailure()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.Must((model, _) => model.Tag == "ok").WithMessage("tag not ok");

        // Act
        var result = rules[0](new TestModel { Name = "value", Tag = "bad" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tag not ok", result.ErrorMessage);
    }

    [Fact]
    public void Must_WithInstance_WhenPredicatePasses_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Name);
        builder.Must((model, _) => model.Tag == "ok");

        // Act
        var result = rules[0](new TestModel { Name = "value", Tag = "ok" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NotNull_WhenNestedProperty_ShouldReturnDottedPropertyName()
    {
        // Arrange
        var (rules, builder) = Build<string?>(model => model.Nested!.Value);
        builder.NotNull();

        // Act
        var result = rules[0](new TestModel { Nested = new NestedModel { Value = null } });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Nested.Value", result.MemberNames);
    }

    [Fact]
    public void Must_WhenNonMemberExpression_ShouldReturnEmptyMemberName()
    {
        // Arrange
        var (rules, builder) = Build(_ => 42);
        builder.Must(_ => false);

        // Act
        var result = rules[0](new TestModel());

        // Assert
        Assert.NotNull(result);
        Assert.Contains(string.Empty, result.MemberNames);
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, TProperty>) Build<TProperty>(Expression<Func<TestModel, TProperty>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        var builder = new RuleBuilder<TestModel, TProperty>(selector, rules);
        return (rules, builder);
    }

    public sealed class TestModel
    {
        public string? Name { get; set; }
        public string? Tag { get; set; }
        public NestedModel? Nested { get; set; }
    }

    public sealed class NestedModel
    {
        public string? Value { get; set; }
    }
}
