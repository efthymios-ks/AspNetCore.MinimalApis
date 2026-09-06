using Microsoft.AspNetCore.MinimalApis.ApiSwagger;

namespace Samples.MinimalApis.Endpoints.GetProducts;

public sealed class GetProductsEndpointSummary : ApiSummary<GetProductsEndpoint>
{
    private static bool _isConfigured;
    private static string _summary = null!;
    private static string _description = null!;

    public GetProductsEndpointSummary()
    {
        Summary = _summary;
        Description = _description;
    }

    public override Task ConfigureAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        if (_isConfigured)
        {
            return Task.CompletedTask;
        }

        _isConfigured = true;

        var section = configuration.GetSection("EndpointSummaries:GetProducts");

        _summary = section.GetValue<string>("Summary")
            ?? throw new InvalidOperationException("Summary is not configured.");

        _description = section.GetValue<string>("Description")
            ?? throw new InvalidOperationException("Description is not configured.");

        return Task.CompletedTask;
    }
}
