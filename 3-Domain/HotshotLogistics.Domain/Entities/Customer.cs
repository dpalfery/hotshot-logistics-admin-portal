
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Domain.Entities;

/// <summary>
/// Represents a customer in the system.
/// </summary>
public class Customer 
{
    /// <inheritdoc/>
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string CompanyName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? TaxId { get; set; }

    /// <inheritdoc/>
    public string? Email { get; set; }

    /// <inheritdoc/>
    public string? Phone { get; set; }

    /// <inheritdoc/>
    public Address BillingAddress { get; set; } = new Address();

    /// <inheritdoc/>
    public List<Contact> Contacts { get; set; } = new List<Contact>();

    /// <inheritdoc/>
    public CreditTerms CreditTerms { get; set; } = new CreditTerms();

    /// <inheritdoc/>
    public decimal CreditLimit { get; set; }

    /// <inheritdoc/>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class.
    /// </summary>
    public Customer()
    {
        if (CreatedAt == default)
            CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the primary contact for the customer.
    /// </summary>
    /// <returns>The primary contact, or null if no primary contact exists.</returns>
    public Contact? GetPrimaryContact()
    {
        return Contacts.FirstOrDefault(c => c.IsPrimary);
    }

    /// <summary>
    /// Adds a contact to the customer.
    /// </summary>
    /// <param name="contact">The contact to add.</param>
    public void AddContact(Contact contact)
    {
        if (contact.IsPrimary)
        {
            // Remove primary flag from existing primary contact
            var existingPrimary = Contacts.FirstOrDefault(c => c.IsPrimary);
            if (existingPrimary != null)
            {
                existingPrimary.IsPrimary = false;
            }
        }

        Contacts.Add(contact);
    }

    /// <summary>
    /// Removes a contact from the customer.
    /// </summary>
    /// <param name="contact">The contact to remove.</param>
    public void RemoveContact(Contact contact)
    {
        Contacts.Remove(contact);
    }

    /// <summary>
    /// Updates the credit terms for the customer.
    /// </summary>
    /// <param name="terms">The new credit terms.</param>
    public void UpdateCreditTerms(CreditTerms terms)
    {
        CreditTerms = terms;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the credit limit for the customer.
    /// </summary>
    /// <param name="newLimit">The new credit limit.</param>
    public void UpdateCreditLimit(decimal newLimit)
    {
        CreditLimit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the customer account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the customer account.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the customer data.
    /// </summary>
    /// <returns>True if the customer data is valid, false otherwise.</returns>
    public bool IsValid()
    {
        // Relaxed validation - Contacts not required for GET operations
        return !string.IsNullOrWhiteSpace(CompanyName) &&
                !string.IsNullOrWhiteSpace(Id) &&
                BillingAddress != null &&
                CreditLimit >= 0 &&
                !string.IsNullOrWhiteSpace(Email);
    }
}
