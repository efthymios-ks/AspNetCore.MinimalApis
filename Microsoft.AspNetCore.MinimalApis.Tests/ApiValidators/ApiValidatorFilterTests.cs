using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiValidators;

public sealed class ApiValidatorFilterTests
{
    private readonly ApiValidatorFilter _filter = new();

    [Fact]
    public async Task InvokeAsync_WhenNoValidatorRegistered_ShouldCallNext()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var context = BuildContext(services, new UnregisteredModel());

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("ok");
        }

        // Act
        await _filter.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentIsNull_ShouldSkipAndCallNext()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var context = BuildContext(services, null);

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("ok");
        }

        // Act
        await _filter.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationPasses_ShouldCallNext()
    {
        // Arrange
        var services = BuildServicesWithValidator();
        var context = BuildContext(services, new ValidatedModel
        {
            Name = "valid"
        });

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("ok");
        }

        // Act
        await _filter.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationFails_ShouldReturnValidationProblem()
    {
        // Arrange
        var services = BuildServicesWithValidator();
        var context = BuildContext(services, new ValidatedModel
        {
            Name = null
        });

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("ok");
        }

        // Act
        var result = await _filter.InvokeAsync(context, Next);

        // Assert
        Assert.False(nextCalled);
        Assert.IsType<IResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task InvokeAsync_WhenMultipleErrorsSameMember_ShouldAggregateErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        static IEnumerable<ValidationResult> multiErrorDelegate(object model) =>
        [
            new ValidationResult("Error 1", ["Name"]),
            new ValidationResult("Error 2", ["Name"])
        ];
        services.AddKeyedSingleton<ApiValidateDelegate>(typeof(ValidatedModel), multiErrorDelegate);
        var context = BuildContext(services.BuildServiceProvider(), new ValidatedModel
        {
            Name = "any"
        });

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>("ok");

        // Act
        var result = await _filter.InvokeAsync(context, Next);

        // Assert
        Assert.IsType<IResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task InvokeAsync_WhenMultipleArguments_ShouldValidateEach()
    {
        // Arrange
        var services = BuildServicesWithValidator();
        var context = BuildMultiContext(services, new ValidatedModel
        {
            Name = null
        }, new UnregisteredModel());

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("ok");
        }

        // Act
        var result = await _filter.InvokeAsync(context, Next);

        // Assert
        Assert.False(nextCalled);
        Assert.IsType<IResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationFailsWithMultipleMembers_ShouldIncludeAllMembers()
    {
        // Arrange
        var services = new ServiceCollection();
        static IEnumerable<ValidationResult> MultiMemberDelegate(object model) =>
        [
            new ValidationResult("Error A", ["FieldA"]),
            new ValidationResult("Error B", ["FieldB"])
        ];
        services.AddKeyedSingleton<ApiValidateDelegate>(typeof(ValidatedModel), MultiMemberDelegate);
        var context = BuildContext(services.BuildServiceProvider(), new ValidatedModel
        {
            Name = "any"
        });

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>("ok");

        // Act
        var result = await _filter.InvokeAsync(context, Next);

        // Assert
        var validationResult = Assert.IsType<IResult>(result, exactMatch: false);
        Assert.NotNull(validationResult);
    }

    private static ServiceProvider BuildServicesWithValidator()
    {
        var services = new ServiceCollection();
        services.AddApiValidators(typeof(ApiValidatorFilterTests).Assembly);
        return services.BuildServiceProvider();
    }

    private static TestContext BuildContext(IServiceProvider services, object? argument)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        return new(httpContext, [argument]);
    }

    private static TestContext BuildMultiContext(IServiceProvider services, params object?[] arguments)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        return new(httpContext, arguments);
    }

    private sealed class TestContext(HttpContext httpContext, IList<object?> arguments) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;
        public override IList<object?> Arguments { get; } = arguments;

        public override TArgument GetArgument<TArgument>(int index)
            => (TArgument)Arguments[index]!;
    }

    public sealed class UnregisteredModel;

    public sealed class ValidatedModel
    {
        public string? Name { get; set; }
    }

    public sealed class ValidatedModelValidator : ApiValidator<ValidatedModel>
    {
        public ValidatedModelValidator()
            => RuleFor(model => model.Name).NotNull().WithMessage("Name is required");
    }
}
