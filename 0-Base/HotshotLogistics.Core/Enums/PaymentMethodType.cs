

namespace HotshotLogistics.Core.Enums
{
    /// <summary>
    /// Represents payment method options for invoice payments.
    /// </summary>
    public enum PaymentMethodType
    {
        /// <summary>
        /// Credit card payment.
        /// </summary>
        CreditCard = 0,

        /// <summary>
        /// ACH bank transfer.
        /// </summary>
        ACH = 1,

        /// <summary>
        /// Paper check.
        /// </summary>
        Check = 2,

        /// <summary>
        /// Cash payment.
        /// </summary>
        Cash = 3,

        /// <summary>
        /// Wire transfer.
        /// </summary>
        WireTransfer = 4,

        /// <summary>
        /// Digital wallet payment.
        /// </summary>
        DigitalWallet = 5
    }
}
