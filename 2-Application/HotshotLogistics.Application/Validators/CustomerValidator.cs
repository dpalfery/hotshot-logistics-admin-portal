using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for customer creation and updates.
/// </summary>
public class CustomerValidator : AbstractValidator<Customer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerValidator"/> class.
    /// </summary>
    public CustomerValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters.");

        RuleFor(x => x.TaxId)
            .MaximumLength(20).WithMessage("Tax ID cannot exceed 20 characters.")
            .Matches(@"^[0-9\-]+$").When(x => !string.IsNullOrEmpty(x.TaxId)).WithMessage("Tax ID can only contain numbers and hyphens.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email)).WithMessage("A valid email address is required.")
            .MaximumLength(254).WithMessage("Email address cannot exceed 254 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?1?[-.\s]?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})$").When(x => !string.IsNullOrEmpty(x.Phone)).WithMessage("A valid US phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.BillingAddress)
            .NotNull().WithMessage("Billing address is required.")
            .SetValidator(new AddressValidator());

        // Contacts are optional - only validate if provided
        RuleFor(x => x.Contacts)
            .Must(x => x == null || x.Count == 0 || x.Any(c => !string.IsNullOrEmpty(c.Email)))
            .WithMessage("If contacts are provided, at least one contact must have an email address.");

        RuleForEach(x => x.Contacts)
            .SetValidator(new ContactValidator())
            .When(x => x.Contacts != null && x.Contacts.Any());

        RuleFor(x => x.CreditTerms)
            .NotNull().WithMessage("Credit terms are required.")
            .SetValidator(new CreditTermsValidator());

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative.")
            .LessThanOrEqualTo(1000000).WithMessage("Credit limit cannot exceed $1,000,000.");

        // Business rule: Ensure only one primary contact (if contacts exist)
        RuleFor(x => x.Contacts)
            .Must(x => x == null || x.Count(c => c.IsPrimary) <= 1)
            .WithMessage("Only one contact can be marked as primary.");
    }
}

/// <summary>
/// Validator for customer creation (stricter than updates).
/// </summary>
public class CreateCustomerValidator : AbstractValidator<Customer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCustomerValidator"/> class.
    /// </summary>
    public CreateCustomerValidator()
    {
        Include(new CustomerValidator());

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required for new customers.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required for new customers.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.BillingAddress)
            .NotNull().WithMessage("Billing address is required for new customers.");

        // Contacts required for new customers
        RuleFor(x => x.Contacts)
            .NotEmpty().WithMessage("At least one contact is required for new customers.");
    }
}

/// <summary>
/// Validator for address information.
/// </summary>
public class AddressValidator : AbstractValidator<Address>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressValidator"/> class.
    /// </summary>
    public AddressValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(100).WithMessage("Street address cannot exceed 100 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(50).WithMessage("City cannot exceed 50 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(50).WithMessage("State cannot exceed 50 characters.");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("A valid US zip code is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(50).WithMessage("Country cannot exceed 50 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees.");
    }
}

/// <summary>
/// Validator for contact information.
/// </summary>
public class ContactValidator : AbstractValidator<Contact>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactValidator"/> class.
    /// </summary>
    public ContactValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(254).WithMessage("Email address cannot exceed 254 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?1?[-.\s]?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})$").When(x => !string.IsNullOrEmpty(x.Phone)).WithMessage("A valid US phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.Title)
            .MaximumLength(50).WithMessage("Title cannot exceed 50 characters.");
    }
}

/// <summary>
/// Validator for credit terms.
/// </summary>
public class CreditTermsValidator : AbstractValidator<CreditTerms>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreditTermsValidator"/> class.
    /// </summary>
    public CreditTermsValidator()
    {
        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).WithMessage("Payment terms days cannot be negative.")
            .LessThanOrEqualTo(120).WithMessage("Payment terms days cannot exceed 120.");

        RuleFor(x => x.ApprovedDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Approved date cannot be in the future.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.ApprovedDate).When(x => x.ExpiryDate.HasValue).WithMessage("Expiry date must be after approved date.")
            .GreaterThan(DateTime.UtcNow).When(x => x.ExpiryDate.HasValue).WithMessage("Expiry date must be in the future.");
    }
}
