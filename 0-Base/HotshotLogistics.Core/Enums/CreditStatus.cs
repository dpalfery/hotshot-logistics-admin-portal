namespace HotshotLogistics.Core.Enums;

/// <summary>
/// Represents the credit status of a customer.
/// </summary>
public enum CreditStatus
{
    /// <summary>
    /// Credit application is pending review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Credit has been approved.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Credit has been denied.
    /// </summary>
    Denied = 2,

    /// <summary>
    /// Credit has been suspended.
    /// </summary>
    Suspended = 3
}
