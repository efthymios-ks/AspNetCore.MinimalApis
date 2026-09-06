using FastEndpoints;
using FluentValidation;
using Samples.Shared.Models;

namespace Samples.FastEndpoints.Endpoints.CreateProduct;

public sealed class CreateProductRequestValidator : Validator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Price)
            .GreaterThan(0);
    }
}
