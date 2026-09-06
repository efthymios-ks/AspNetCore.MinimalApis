using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Microsoft.AspNetCore.MinimalApis.ApiValidators.RuleBuilders;
using Samples.MinimalApis.Endpoints.RegisterCustomer.Request;

namespace Samples.MinimalApis.Endpoints.RegisterCustomer.Validators;

/// <summary>
/// Demonstrates (almost) every available rule and modifier on a single request model.
/// A valid sample lives in <see cref="RegisterCustomerEndpointSummary"/> — break one field
/// at a time to see each rule fire.
/// </summary>
public sealed class RegisterCustomerRequestValidator : ApiValidator<RegisterCustomerRequest>
{
    public RegisterCustomerRequestValidator()
    {
        // --- Strings / formats ---
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required").WithErrorCode("EMAIL_REQUIRED")
            .EmailAddress().WithMessage("Email must be a valid address").WithErrorCode("EMAIL_INVALID");

        RuleFor(request => request.ConfirmEmail)
            .Must((root, value) => value == root.Email).WithMessage("ConfirmEmail must match Email");

        RuleFor(request => request.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_]+$").WithName("username").WithMessage("Username allows letters, digits and underscore only");

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter");

        RuleFor(request => request.DisplayName).NotEmpty().Length(2, 50);
        RuleFor(request => request.Country).NotEmpty().Length(2);
        RuleFor(request => request.PhonePrefix).NotEmpty().StartsWith("+");
        RuleFor(request => request.Website).NotEmpty().AbsoluteUrl();
        RuleFor(request => request.CallbackPath).NotEmpty().RelativeUrl();
        RuleFor(request => request.CardNumber).NotEmpty().CreditCard();
        RuleFor(request => request.FileName).NotEmpty().EndsWith(".pdf");
        RuleFor(request => request.SearchKeywords).NotEmpty().Contains("api");
        RuleFor(request => request.Nickname).NotEmpty().NotEqual("admin");
        RuleFor(request => request.Bio).MaximumLength(500);
        RuleFor(request => request.MiddleName).Null().WithMessage("MiddleName must not be provided");
        RuleFor(request => request.Notes).Empty().WithMessage("Notes must be empty");

        // --- Numerics ---
        RuleFor(request => request.Age).InclusiveBetween(18, 120);
        RuleFor(request => request.Rating).ExclusiveBetween(0, 10);
        RuleFor(request => request.Score).GreaterThan(0);
        RuleFor(request => request.Credits).GreaterThanOrEqualTo(0);
        RuleFor(request => request.DiscountPercent).LessThan(100);
        RuleFor(request => request.Priority).LessThanOrEqualTo(5);
        RuleFor(request => request.AcceptedVersion).Equal(2);
        RuleFor(request => request.Quantity).Positive();
        RuleFor(request => request.Balance).PositiveOrZero();
        RuleFor(request => request.Adjustment).Negative();
        RuleFor(request => request.Penalty).NegativeOrZero();
        RuleFor(request => request.RemainingStrikes).Zero();
        RuleFor(request => request.Multiplier).NotZero();

        // --- Enums ---
        RuleFor(request => request.Status).IsInEnum();
        RuleFor(request => request.Tier).IsInEnum();

        // --- Booleans / conditionals ---
        // Fail only when the value is present AND false (null/missing passes).
        RuleFor(request => request.AcceptedTerms)
            .Must(accepted => accepted is true).WithMessage("You must accept the terms")
            .When(request => request.AcceptedTerms is not null);

        RuleFor(request => request.ReferralCode)
            .NotEmpty().WithMessage("Referral code is required when subscribing")
            .When(request => request.SubscribeToNewsletter);

        RuleFor(request => request.CouponCode)
            .NotEmpty().WithMessage("Coupon code is required")
            .Unless(request => request.IsGuest);

        // --- Collections ---
        RuleFor(request => request.Tags)
            .NotEmpty()
            .ForEach<string>(tag => tag.RuleFor(value => value).NotEmpty().MaximumLength(20));

        RuleFor(request => request.Contacts)
            .ForEach<RegisterCustomerContact>(contact =>
            {
                contact.RuleFor(value => value.Type).NotEmpty();
                contact.RuleFor(value => value.Value).NotEmpty();
            });

        // --- Nested object via SetValidator ---
        RuleFor(request => request.Address)
            .NotNull().WithMessage("Address is required")
            .SetValidator(new RegisterCustomerAddressValidator());

        // --- Nested object via inline ChildRules ---
        RuleFor(request => request.BillingAddress)
            .NotNull().WithMessage("Billing address is required")
            .ChildRules(billing =>
            {
                billing.RuleFor(value => value.Line1).NotEmpty();
                billing.RuleFor(value => value.City).NotEmpty();
            });

        // NOTE on Include: it merges another validator's rules, but a second *discoverable*
        // ApiValidator<RegisterCustomerRequest> would collide here (AddApiValidators registers one
        // validator per argument type, first wins). So this rule is inlined; Include itself is
        // covered by unit tests.
        RuleFor(request => request.TermsVersion).GreaterThan(0);
    }
}
