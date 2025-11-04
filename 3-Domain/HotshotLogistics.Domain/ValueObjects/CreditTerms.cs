using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents credit terms for a customer.
/// </summary>
public class CreditTerms 
{
    /// <summary>
    /// Gets or sets the payment terms in days (e.g., NET 15, NET 30).
    /// </summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>
    /// Gets or sets the credit status.
    /// </summary>
    public CreditStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date when credit was approved.
    /// </summary>
    public DateTime ApprovedDate { get; set; }

    /// <summary>
    /// Gets or sets the date when credit expires.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
}
