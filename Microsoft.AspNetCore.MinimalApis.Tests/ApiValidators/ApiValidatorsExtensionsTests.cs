using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators;

public sealed class ApiValidatorsExtensionsTests
{
    [Fact]
    public void AddApiValidators_WhenValidatorExistsInAssembly_ShouldRegisterKeyedService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiValidators(typeof(ApiValidatorsExtensionsTests).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var validator = provider.GetKeyedService<ApiValidateDelegate>(typeof(SampleModel));
        Assert.NotNull(validator);
    }

    [Fact]
    public void AddApiValidators_WhenNoValidators_ShouldNotRegisterAny()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiValidators(typeof(object).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var validator = provider.GetKeyedService<ApiValidateDelegate>(typeof(SampleModel));
        Assert.Null(validator);
    }

    [Fact]
    public void AddApiValidators_WhenRegistered_ShouldValidateCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApiValidators(typeof(ApiValidatorsExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();
        var validator = provider.GetKeyedService<ApiValidateDelegate>(typeof(SampleModel))!;
        var model = new SampleModel
        {
            Name = null
        };

        // Act
        var results = validator(model)
            .ToArray();

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void AddApiValidators_WhenRegisteredTwice_ShouldNotDuplicate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApiValidators(typeof(ApiValidatorsExtensionsTests).Assembly);

        // Act
        services.AddApiValidators(typeof(ApiValidatorsExtensionsTests).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var validators = provider.GetKeyedServices<ApiValidateDelegate>(typeof(SampleModel));
        Assert.Single(validators);
    }

    public sealed class SampleModel
    {
        public string? Name { get; set; }
    }

    public sealed class SampleModelValidator : ApiValidator<SampleModel>
    {
        public SampleModelValidator()
            => RuleFor(model => model.Name).NotNull().WithMessage("Name is required");
    }
}
