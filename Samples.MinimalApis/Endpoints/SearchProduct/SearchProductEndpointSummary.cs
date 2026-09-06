using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Samples.Shared.Models;

namespace Samples.MinimalApis.Endpoints.SearchProduct;

public sealed class SearchProductEndpointSummary : ApiSummary<SearchProductEndpoint>
{
    public SearchProductEndpointSummary()
    {
        Summary = "Search products by category";
        Description = Summary;

        AddParameterExamples<SearchProductRequest>(new()
        {
            CategoryId = 5,
            Search = "Smartphone"
        });
    }
}
