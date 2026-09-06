namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

public enum IdempotencyStatus
{
    Reserved = 1,
    Pending,
    Completed
}
