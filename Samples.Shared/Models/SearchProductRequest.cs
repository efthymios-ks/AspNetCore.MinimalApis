namespace Samples.Shared.Models;

public sealed class SearchProductRequest
{
    public int CategoryId { get; set; }
    public string? Search { get; set; }
}
