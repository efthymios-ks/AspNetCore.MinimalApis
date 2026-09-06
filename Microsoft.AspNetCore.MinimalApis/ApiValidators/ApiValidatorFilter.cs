using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators;

internal sealed class ApiValidatorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            var argumentType = argument.GetType();
            var validateFunc = context
                .HttpContext
                .RequestServices
                .GetKeyedService<ApiValidateDelegate>(serviceKey: argumentType);
            if (validateFunc is null)
            {
                continue;
            }

            var validationResults = validateFunc!(argument).ToArray();
            if (validationResults.Length == 0)
            {
                continue;
            }

            var errors = new Dictionary<string, string[]>();
            foreach (var validationResult in validationResults)
            {
                var errorMessage = validationResult.ErrorMessage ?? string.Empty;
                foreach (var memberName in validationResult.MemberNames)
                {
                    if (!errors.TryGetValue(memberName, out var value))
                    {
                        errors[memberName] = [errorMessage];
                    }
                    else
                    {
                        var existingErrors = value.ToList();
                        existingErrors.Add(errorMessage);
                        errors[memberName] = [.. existingErrors];
                    }
                }
            }

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
