namespace Samples.Shared.Models;

public sealed class CreateProductRequest
{
    public required string Name { get; set; } = string.Empty;
    public required decimal Price { get; set; }
}
