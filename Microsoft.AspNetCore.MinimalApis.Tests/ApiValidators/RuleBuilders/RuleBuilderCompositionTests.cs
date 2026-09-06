using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Microsoft.AspNetCore.MinimalApis.ApiValidators.RuleBuilders;
using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators.RuleBuilders;

public sealed class RuleBuilderCompositionTests
{
    [Fact]
    public void SetValidator_WhenChildInvalid_ShouldReturnPrefixedFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Address);
        builder.SetValidator(new AddressValidator());

        // Act
        var result = rules[0](new TestModel { Address = new Address { Street = null } });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Address.Street", result.MemberNames);
    }

    [Fact]
    public void SetValidator_WhenChildValid_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Address);
        builder.SetValidator(new AddressValidator());

        // Act
        var result = rules[0](new TestModel { Address = new Address { Street = "Main" } });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetValidator_WhenPropertyIsNull_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Address);
        builder.SetValidator(new AddressValidator());

        // Act
        var result = rules[0](new TestModel { Address = null! });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ChildRules_WhenChildInvalid_ShouldReturnPrefixedFailure()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Address);
        builder.ChildRules(address => address.RuleFor(value => value.Street).NotNull());

        // Act
        var result = rules[0](new TestModel { Address = new Address { Street = null } });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Address.Street", result.MemberNames);
    }

    [Fact]
    public void ChildRules_WhenChildValid_ShouldReturnNull()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Address);
        builder.ChildRules(address => address.RuleFor(value => value.Street).NotNull());

        // Act
        var result = rules[0](new TestModel { Address = new Address { Street = "Main" } });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetValidator_WhenChildFailureHasErrorCode_ShouldPreserveCode()
    {
        // Arrange
        var (rules, builder) = Build(model => model.Address);
        builder.ChildRules(address => address.RuleFor(value => value.Street).NotNull().WithErrorCode("STREET"));

        // Act
        var result = rules[0](new TestModel { Address = new Address { Street = null } });

        // Assert
        var apiResult = Assert.IsType<ApiValidationResult>(result);
        Assert.Equal("STREET", apiResult.ErrorCode);
        Assert.Contains("Address.Street", apiResult.MemberNames);
    }

    private static (List<Func<TestModel, ValidationResult?>>, IRuleBuilder<TestModel, Address>) Build(
        Expression<Func<TestModel, Address>> selector)
    {
        var rules = new List<Func<TestModel, ValidationResult?>>();
        return (rules, new RuleBuilder<TestModel, Address>(selector, rules));
    }

    private sealed class AddressValidator : ApiValidator<Address>
    {
        public AddressValidator()
            => RuleFor(address => address.Street).NotNull();
    }

    public sealed class TestModel
    {
        public Address Address { get; set; } = new();
    }

    public sealed class Address
    {
        public string? Street { get; set; }
    }
}
