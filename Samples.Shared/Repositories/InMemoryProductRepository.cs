using Samples.Shared.Models;

namespace Samples.Shared.Repositories;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ICollection<Product> _products = [];
    private int _nextId = 1;

    public Task<IEnumerable<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = Enumerable.Range(1, 5).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Price = i * 10.0m
        });
        return Task.FromResult(products);
    }

    public Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.Id = _nextId++;
        _products.Add(product);
        return Task.FromResult(product);
    }
}
