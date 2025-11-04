// <copyright file="InvoiceExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;
    using HotshotLogistics.Core.Enums;


    /// <summary>
    /// Extension methods for invoice operations.
    /// </summary>
    public static class InvoiceExtensions
    {
        /// <summary>
        /// Checks if the invoice is overdue.
        /// </summary>
        /// <param name="invoice">The invoice to check.</param>
        /// <returns>True if the invoice is overdue, false otherwise.</returns>
        public static bool IsOverdue(this Invoice invoice)
        {
            if (invoice == null)
                return false;

            return DateTime.UtcNow.Date > invoice.DueDate.Date && invoice.BalanceDue > 0;
        }

        /// <summary>
        /// Gets the number of days overdue.
        /// </summary>
        /// <param name="invoice">The invoice to check.</param>
        /// <returns>The number of days overdue, or 0 if not overdue.</returns>
        public static int DaysOverdue(this Invoice invoice)
        {
            if (invoice == null || !invoice.IsOverdue())
                return 0;

            return (DateTime.UtcNow.Date - invoice.DueDate.Date).Days;
        }

        /// <summary>
        /// Checks if the invoice is eligible for early payment discount.
        /// </summary>
        /// <param name="invoice">The invoice to check.</param>
        /// <returns>True if eligible for early payment discount, false otherwise.</returns>
        public static bool IsEligibleForEarlyDiscount(this Invoice invoice)
        {
            if (invoice == null || invoice.Terms == null)
                return false;

            if (invoice.Terms.EarlyPaymentDiscount <= 0 || invoice.Terms.EarlyPaymentDiscountDays <= 0)
                return false;

            var discountDeadline = invoice.InvoiceDate.AddDays(invoice.Terms.EarlyPaymentDiscountDays);
            return DateTime.UtcNow.Date <= discountDeadline.Date && invoice.BalanceDue > 0;
        }

        /// <summary>
        /// Calculates the early payment discount amount.
        /// </summary>
        /// <param name="invoice">The invoice to calculate discount for.</param>
        /// <returns>The early payment discount amount.</returns>
        public static decimal CalculateEarlyPaymentDiscount(this Invoice invoice)
        {
            if (!invoice.IsEligibleForEarlyDiscount())
                return 0;

            return invoice.TotalAmount * invoice.Terms.EarlyPaymentDiscount;
        }

        /// <summary>
        /// Gets a formatted string representation of the invoice status.
        /// </summary>
        /// <param name="invoice">The invoice.</param>
        /// <returns>A formatted status string.</returns>
        public static string GetStatusDisplay(this Invoice invoice)
        {
            if (invoice == null)
                return "Unknown";

            return invoice.Status switch
            {
                InvoiceStatus.Draft => "Draft",
                InvoiceStatus.Sent => "Sent",
                InvoiceStatus.Viewed => "Viewed",
                InvoiceStatus.Paid => "Paid",
                InvoiceStatus.Overdue => "Overdue",
                InvoiceStatus.PartiallyPaid => "Partially Paid",
                InvoiceStatus.Cancelled => "Cancelled",
                InvoiceStatus.Disputed => "Disputed",
                _ => invoice.Status.ToString()
            };
        }
    }
}
