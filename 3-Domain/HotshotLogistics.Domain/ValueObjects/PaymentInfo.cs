namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents payment information for a driver.
/// </summary>
public class PaymentInfo
{
    /// <summary>
    /// Gets or sets the hourly rate.
    /// </summary>
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Gets or sets the per-mile rate.
    /// </summary>
    public decimal PerMileRate { get; set; }

    /// <summary>
    /// Gets or sets the bank account number (encrypted).
    /// </summary>
    public string BankAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing number.
    /// </summary>
    public string RoutingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax identification number.
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment method preference.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }
}
