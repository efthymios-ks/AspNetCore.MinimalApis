using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators;

public sealed class ApiValidatorWhenTests
{
    private sealed class Model
    {
        public bool Flag { get; set; }
        public string? Name { get; set; }
    }

    private sealed class BlockWhenValidator : ApiValidator<Model>
    {
        public BlockWhenValidator()
            => When(model => model.Flag, () => RuleFor(model => model.Name).NotEmpty());
    }

    private sealed class BlockUnlessValidator : ApiValidator<Model>
    {
        public BlockUnlessValidator()
            => Unless(model => model.Flag, () => RuleFor(model => model.Name).NotEmpty());
    }

    [Fact]
    public void When_ConditionTrue_RuleApplies()
        => Assert.NotEmpty(new BlockWhenValidator().Validate(new Model { Flag = true, Name = null }));

    [Fact]
    public void When_ConditionFalse_RuleSkipped()
        => Assert.Empty(new BlockWhenValidator().Validate(new Model { Flag = false, Name = null }));

    [Fact]
    public void Unless_ConditionTrue_RuleSkipped()
        => Assert.Empty(new BlockUnlessValidator().Validate(new Model { Flag = true, Name = null }));

    [Fact]
    public void Unless_ConditionFalse_RuleApplies()
        => Assert.NotEmpty(new BlockUnlessValidator().Validate(new Model { Flag = false, Name = null }));
}
