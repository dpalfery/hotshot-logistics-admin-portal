

namespace HotshotLogistics.Domain.ValueObjects
{
    public class PaymentTerms
    {
        /// <summary>
        /// Gets or sets the payment terms in days (e.g., NET 15, NET 30).
        /// </summary>
        public int Days { get; set; }

        /// <summary>
        /// Gets or sets the early payment discount percentage.
        /// </summary>
        public decimal EarlyPaymentDiscount { get; set; }

        /// <summary>
        /// Gets or sets the early payment discount days.
        /// </summary>
        public int EarlyPaymentDiscountDays { get; set; }

        /// <summary>
        /// Gets or sets the late payment penalty percentage.
        /// </summary>
        public decimal LatePaymentPenalty { get; set; }

        /// <summary>
        /// Gets or sets the late payment penalty days.
        /// </summary>
        public int LatePaymentPenaltyDays { get; set; }
    }
}
