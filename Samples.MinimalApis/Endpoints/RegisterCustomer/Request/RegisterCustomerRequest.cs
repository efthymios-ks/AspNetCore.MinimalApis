namespace Samples.MinimalApis.Endpoints.RegisterCustomer.Request;

public sealed class RegisterCustomerRequest
{
    // String / format rules
    public string? Email { get; set; }
    public string? ConfirmEmail { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? Country { get; set; }
    public string? PhonePrefix { get; set; }
    public string? Website { get; set; }
    public string? CallbackPath { get; set; }
    public string? CardNumber { get; set; }
    public string? FileName { get; set; }
    public string? SearchKeywords { get; set; }
    public string? Nickname { get; set; }
    public string? Bio { get; set; }
    public string? MiddleName { get; set; }
    public string? Notes { get; set; }

    // Numeric rules
    public int Age { get; set; }
    public int Rating { get; set; }
    public int Score { get; set; }
    public int Credits { get; set; }
    public int DiscountPercent { get; set; }
    public int Priority { get; set; }
    public int AcceptedVersion { get; set; }
    public int Quantity { get; set; }
    public decimal Balance { get; set; }
    public int Adjustment { get; set; }
    public int Penalty { get; set; }
    public int RemainingStrikes { get; set; }
    public int Multiplier { get; set; }

    // Enum rules
    public CustomerStatus Status { get; set; }
    public CustomerTier Tier { get; set; }

    // Boolean / conditional rules
    public bool? AcceptedTerms { get; set; }
    public bool SubscribeToNewsletter { get; set; }
    public string? ReferralCode { get; set; }
    public bool IsGuest { get; set; }
    public string? CouponCode { get; set; }

    // Collections + nested
    public List<string> Tags { get; set; } = [];
    public List<RegisterCustomerContact> Contacts { get; set; } = [];
    public RegisterCustomerAddress Address { get; set; } = null!;
    public RegisterCustomerBillingAddress BillingAddress { get; set; } = null!;

    // Included base-validator rule
    public int TermsVersion { get; set; }
}
