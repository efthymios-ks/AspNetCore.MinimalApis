using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Samples.MinimalApis.Endpoints.RegisterCustomer.Request;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.RegisterCustomer;

public sealed class RegisterCustomerEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/customers", Handle)
            .Accepts<RegisterCustomerRequest>(MediaTypeNames.Application.Json)
            .Produces<RegisterCustomerResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithTags("Customers");

    private static IResult Handle(RegisterCustomerRequest request)
        => Results.Ok(new RegisterCustomerResponse
        {
            Message = "Customer registered",
            Email = request.Email
        });
}
