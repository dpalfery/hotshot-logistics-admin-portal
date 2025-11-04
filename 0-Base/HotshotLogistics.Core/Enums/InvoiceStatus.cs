namespace HotshotLogistics.Core.Enums
{
    /// <summary>
    /// Represents the lifecycle status of an invoice.
    /// </summary>
    public enum InvoiceStatus
    {
        /// <summary>
        /// Invoice has been drafted but not sent.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Invoice has been sent to the recipient.
        /// </summary>
        Sent = 1,

        /// <summary>
        /// Invoice has been viewed by the recipient.
        /// </summary>
        Viewed = 2,

        /// <summary>
        /// Invoice has been partially paid.
        /// </summary>
        PartiallyPaid = 3,

        /// <summary>
        /// Invoice has been paid in full.
        /// </summary>
        Paid = 4,

        /// <summary>
        /// Invoice is overdue.
        /// </summary>
        Overdue = 5,

        /// <summary>
        /// Invoice has been cancelled.
        /// </summary>
        Cancelled = 6,

        /// <summary>
        /// Invoice is disputed.
        /// </summary>
        Disputed = 7
    }
}
