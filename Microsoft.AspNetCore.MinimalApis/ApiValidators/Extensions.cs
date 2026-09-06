using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators;

public static class Extensions
{
    internal static IServiceCollection AddApiValidators(this IServiceCollection services)
        => services.AddApiValidators(Assembly.GetEntryAssembly()!);

    internal static IServiceCollection AddApiValidators(this IServiceCollection services, params Assembly[] assemblies)
    {
        var validatorTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Aggregate(new Dictionary<Type, Type>(), (validators, type) =>
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    return validators;
                }

                while (type.BaseType is not null)
                {
                    var baseType = type.BaseType;
                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ApiValidator<>))
                    {
                        validators[type] = baseType;
                        return validators;
                    }

                    type = baseType;
                }

                return validators;
            })
            .Select(validator =>
            {
                var validatorType = validator.Key;
                var validatorBaseType = validator.Value;
                var genericArguments = validatorBaseType.GetGenericArguments();
                var genericArgumentType = genericArguments[0];
                var validatorInstance = Activator.CreateInstance(validatorType);
                var validateMethod = validatorType.GetMethod(
                    nameof(ApiValidator<>.Validate),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                )!;

                IEnumerable<ValidationResult> Validate(object argument)
                    => (IEnumerable<ValidationResult>)validateMethod.Invoke(validatorInstance, [argument])!;

                return new
                {
                    ServiceKey = genericArgumentType,
                    ServiceInstance = (ApiValidateDelegate)Validate
                };
            });

        foreach (var validator in validatorTypes)
        {
            services.TryAddKeyedSingleton(
                serviceKey: validator.ServiceKey,
                instance: validator.ServiceInstance
            );
        }

        return services;
    }
}
