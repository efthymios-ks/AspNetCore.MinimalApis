using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Samples.Shared.Models;

namespace Samples.MinimalApis.Endpoints.CreateProduct;

public sealed class CreateProductRequestValidator
    : ApiValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Price)
            .Positive();
    }
}
