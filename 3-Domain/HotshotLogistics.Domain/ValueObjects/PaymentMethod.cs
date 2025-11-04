namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents payment method options.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Direct deposit to bank account.
    /// </summary>
    DirectDeposit = 0,

    /// <summary>
    /// Paper check.
    /// </summary>
    Check = 1,

    /// <summary>
    /// PayPal or digital wallet.
    /// </summary>
    DigitalWallet = 2
}
