using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Samples.MinimalApis.Endpoints.RegisterCustomer.Request;

namespace Samples.MinimalApis.Endpoints.RegisterCustomer;

public sealed class RegisterCustomerEndpointSummary : ApiSummary<RegisterCustomerEndpoint>
{
    public RegisterCustomerEndpointSummary()
    {
        Summary = "Register a customer (validation showcase)";
        Description = "Exercises nearly every ApiValidator rule on a single request model. "
            + "POST the valid sample below, then break one field at a time to watch each rule fire.";

        AddBodyExample("Valid request", ValidSample);
        AddResponseExample(
            StatusCodes.Status200OK,
            "Registered",
            new RegisterCustomerResponse
            {
                Message = "Customer registered",
                Email = "jane.doe@example.com"
            }
        );
    }

    // A request that passes every rule. Break one field to see the corresponding rule fail.
    public static RegisterCustomerRequest ValidSample { get; } = new()
    {
        Email = "jane.doe@example.com",
        ConfirmEmail = "jane.doe@example.com",
        Username = "jane_doe",
        Password = "Secret123",
        DisplayName = "Jane Doe",
        Country = "US",
        PhonePrefix = "+1",
        Website = "https://example.com",
        CallbackPath = "/callback",
        CardNumber = "4111111111111111",
        FileName = "report.pdf",
        SearchKeywords = "api docs",
        Nickname = "jd",
        Bio = "Hello there",
        MiddleName = null,
        Notes = null,
        Age = 30,
        Rating = 5,
        Score = 50,
        Credits = 0,
        DiscountPercent = 10,
        Priority = 3,
        AcceptedVersion = 2,
        Quantity = 3,
        Balance = 100m,
        Adjustment = -5,
        Penalty = 0,
        RemainingStrikes = 0,
        Multiplier = 2,
        Status = CustomerStatus.Active,
        Tier = CustomerTier.Gold,
        AcceptedTerms = true,
        SubscribeToNewsletter = true,
        ReferralCode = "REF123",
        IsGuest = false,
        CouponCode = "SAVE10",
        Tags = ["new", "priority"],
        Contacts = [new RegisterCustomerContact { Type = "email", Value = "jane@example.com" }],
        Address = new RegisterCustomerAddress { Street = "1 Main St", PostalCode = "12345" },
        BillingAddress = new RegisterCustomerBillingAddress { Line1 = "PO Box 9", City = "Metropolis" },
        TermsVersion = 1
    };
}
