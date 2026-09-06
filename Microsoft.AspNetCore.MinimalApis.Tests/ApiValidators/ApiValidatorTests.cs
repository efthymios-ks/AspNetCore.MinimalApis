using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators;

public sealed class ApiValidatorTests
{
    [Fact]
    public void Validate_WhenArgumentIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var validator = new TestValidator();

        // Act
        IEnumerable<ValidationResult> Act()
            => validator.Validate(null!);

        // Assert
        Assert.Throws<ArgumentNullException>((Func<IEnumerable<ValidationResult>>)Act);
    }

    [Fact]
    public void Validate_WhenNoRulesFail_ShouldReturnEmpty()
    {
        // Arrange
        var validator = new TestValidator();
        var model = new TestModel
        {
            Name = "valid"
        };

        // Act
        var results = validator.Validate(model)
            .ToArray();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WhenRuleFails_ShouldReturnValidationResult()
    {
        // Arrange
        var validator = new TestValidator();
        var model = new TestModel
        {
            Name = null
        };

        // Act
        var results = validator.Validate(model)
            .ToArray();

        // Assert
        Assert.Single(results);
        Assert.Contains("Name", results[0].MemberNames);
    }

    [Fact]
    public void RuleFor_WhenCalled_ShouldReturnWorkingRuleBuilder()
    {
        // Arrange
        var validator = new TestValidatorWithChain();
        var model = new TestModel
        {
            Name = null
        };

        // Act
        var results = validator.Validate(model)
            .ToArray();

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Include_ShouldMergeRulesFromOtherValidator()
    {
        // Arrange
        var validator = new IncludingValidator();
        var model = new TestModel { Name = null };

        // Act
        var results = validator.Validate(model).ToArray();

        // Assert
        Assert.Single(results);
        Assert.Contains("Name", results[0].MemberNames);
    }

    private sealed class TestValidator : ApiValidator<TestModel>
    {
        public TestValidator()
            => RuleFor(model => model.Name).NotNull();

        public new IEnumerable<ValidationResult> Validate(TestModel argument)
            => base.Validate(argument);
    }

    private sealed class BaseValidator : ApiValidator<TestModel>
    {
        public BaseValidator()
            => RuleFor(model => model.Name).NotNull();
    }

    private sealed class IncludingValidator : ApiValidator<TestModel>
    {
        public IncludingValidator()
            => Include(new BaseValidator());

        public new IEnumerable<ValidationResult> Validate(TestModel argument)
            => base.Validate(argument);
    }

    private sealed class TestValidatorWithChain : ApiValidator<TestModel>
    {
        public TestValidatorWithChain()
        {
            RuleFor(model => model.Name)
                .NotNull()
                .Must(name => name?.Length > 0);
        }

        public new IEnumerable<ValidationResult> Validate(TestModel argument)
            => base.Validate(argument);
    }

    public sealed class TestModel
    {
        public string? Name { get; set; }
    }
}
