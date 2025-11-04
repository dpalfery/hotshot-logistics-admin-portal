using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotshotLogistics.Core.Enums
{
    /// <summary>
    /// Represents the status of a payment.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Payment is pending processing.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Payment has been completed successfully.
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Payment has failed.
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Payment has been refunded.
        /// </summary>
        Refunded = 3,

        /// <summary>
        /// Payment is being processed.
        /// </summary>
        Processing = 4,

        /// <summary>
        /// Payment has been cancelled.
        /// </summary>
        Cancelled = 5
    }
}
