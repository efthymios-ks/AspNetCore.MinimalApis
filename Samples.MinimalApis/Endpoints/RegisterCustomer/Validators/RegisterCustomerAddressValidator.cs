using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Samples.MinimalApis.Endpoints.RegisterCustomer.Request;

namespace Samples.MinimalApis.Endpoints.RegisterCustomer.Validators;

public sealed class RegisterCustomerAddressValidator : ApiValidator<RegisterCustomerAddress>
{
    public RegisterCustomerAddressValidator()
    {
        RuleFor(address => address.Street).NotEmpty();
        RuleFor(address => address.PostalCode).NotEmpty().Matches(@"^\d{5}$").WithMessage("PostalCode must be 5 digits");
    }
}
