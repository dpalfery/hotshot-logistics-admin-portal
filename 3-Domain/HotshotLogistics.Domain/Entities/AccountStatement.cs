// <copyright file="AccountStatement.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents an account statement for a customer.
    /// </summary>
    public class AccountStatement
    {
        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the statement date.
        /// </summary>
        public DateTime StatementDate { get; set; }

        /// <summary>
        /// Gets or sets the period start date.
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the period end date.
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets the list of invoices in this statement period.
        /// </summary>
        public List<Invoice> Invoices { get; set; } = new List<Invoice>();

        /// <summary>
        /// Gets or sets the total amount invoiced in this period.
        /// </summary>
        public decimal TotalInvoiced { get; set; }

        /// <summary>
        /// Gets or sets the total amount paid in this period.
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        /// Gets or sets the total outstanding balance.
        /// </summary>
        public decimal TotalOutstanding { get; set; }

        /// <summary>
        /// Gets or sets the overdue amount.
        /// </summary>
        public decimal OverdueAmount { get; set; }

        /// <summary>
        /// Gets the aging breakdown of outstanding invoices.
        /// </summary>
        public AgingBreakdown AgingBreakdown => CalculateAgingBreakdown();

        /// <summary>
        /// Gets the number of invoices in the statement.
        /// </summary>
        public int InvoiceCount => Invoices.Count;

        /// <summary>
        /// Gets the number of overdue invoices.
        /// </summary>
        public int OverdueInvoiceCount => Invoices.Count(i => i.IsOverdue());

        /// <summary>
        /// Calculates the aging breakdown of outstanding invoices.
        /// </summary>
        /// <returns>The aging breakdown.</returns>
        private AgingBreakdown CalculateAgingBreakdown()
        {
            var breakdown = new AgingBreakdown();
            var today = DateTime.UtcNow.Date;

            foreach (var invoice in Invoices.Where(i => i.BalanceDue > 0))
            {
                var daysOverdue = (today - invoice.DueDate.Date).Days;

                if (daysOverdue <= 0)
                {
                    breakdown.Current += invoice.BalanceDue;
                }
                else if (daysOverdue <= 30)
                {
                    breakdown.Days1To30 += invoice.BalanceDue;
                }
                else if (daysOverdue <= 60)
                {
                    breakdown.Days31To60 += invoice.BalanceDue;
                }
                else if (daysOverdue <= 90)
                {
                    breakdown.Days61To90 += invoice.BalanceDue;
                }
                else
                {
                    breakdown.Over90Days += invoice.BalanceDue;
                }
            }

            return breakdown;
        }
    }

    /// <summary>
    /// Represents the aging breakdown of outstanding invoices.
    /// </summary>
    public class AgingBreakdown
    {
        /// <summary>
        /// Gets or sets the current amount (not yet due).
        /// </summary>
        public decimal Current { get; set; }

        /// <summary>
        /// Gets or sets the amount 1-30 days overdue.
        /// </summary>
        public decimal Days1To30 { get; set; }

        /// <summary>
        /// Gets or sets the amount 31-60 days overdue.
        /// </summary>
        public decimal Days31To60 { get; set; }

        /// <summary>
        /// Gets or sets the amount 61-90 days overdue.
        /// </summary>
        public decimal Days61To90 { get; set; }

        /// <summary>
        /// Gets or sets the amount over 90 days overdue.
        /// </summary>
        public decimal Over90Days { get; set; }

        /// <summary>
        /// Gets the total outstanding amount.
        /// </summary>
        public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90Days;
    }
}
