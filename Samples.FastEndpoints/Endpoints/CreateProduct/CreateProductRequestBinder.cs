using FastEndpoints;
using Samples.Shared.Models;

namespace Samples.FastEndpoints.Endpoints.CreateProduct;

public sealed class CreateProductRequestBinder : RequestBinder<CreateProductRequest>
{
    public override ValueTask<CreateProductRequest> BindAsync(BinderContext ctx, CancellationToken cancellation)
        => base.BindAsync(ctx, cancellation);
}