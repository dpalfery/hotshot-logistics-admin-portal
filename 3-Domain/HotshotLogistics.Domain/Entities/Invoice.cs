// <copyright file="Invoice.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Domain.ValueObjects;
using HotshotLogistics.Core.Enums;
namespace HotshotLogistics.Domain.Entities
{

    /// <summary>
    /// Represents an invoice in the system.
    /// </summary>
    public class Invoice 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Invoice"/> class.
        /// </summary>
        public Invoice()
        {
            Id = Guid.NewGuid().ToString();
            LineItems = new List<InvoiceLineItem>();
            Terms = new PaymentTerms();
            CreatedAt = DateTime.UtcNow;
            Status = InvoiceStatus.Draft;
        }

        /// <inheritdoc/>
        public string Id { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string CustomerId { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string? JobId { get; set; }

        /// <inheritdoc/>
        public DateTime InvoiceDate { get; set; }

        /// <inheritdoc/>
        public DateTime DueDate { get; set; }

        /// <inheritdoc/>
        public InvoiceStatus Status { get; set; }

        /// <inheritdoc/>
        public List<InvoiceLineItem> LineItems { get; set; }

        /// <inheritdoc/>
        public decimal SubTotal { get; set; }

        /// <inheritdoc/>
        public decimal TaxRate { get; set; }

        /// <inheritdoc/>
        public decimal TaxAmount { get; set; }

        /// <inheritdoc/>
        public decimal DiscountAmount { get; set; }

        /// <inheritdoc/>
        public decimal TotalAmount { get; set; }

        /// <inheritdoc/>
        public decimal PaidAmount { get; set; }

        /// <inheritdoc/>
        public decimal BalanceDue => TotalAmount - PaidAmount;

        /// <inheritdoc/>
        public PaymentTerms Terms { get; set; } = new PaymentTerms();

        /// <inheritdoc/>
        public string Notes { get; set; } = string.Empty;

        /// <inheritdoc/>
        public DateTime CreatedAt { get; set; }

        /// <inheritdoc/>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Adds a line item to the invoice.
        /// </summary>
        /// <param name="lineItem">The line item to add.</param>
        public void AddLineItem(InvoiceLineItem lineItem)
        {
            if (lineItem == null)
                throw new ArgumentNullException(nameof(lineItem));

            lineItem.SortOrder = LineItems.Count + 1;
            LineItems.Add(lineItem);
            CalculateTotals();
        }

        /// <summary>
        /// Removes a line item from the invoice.
        /// </summary>
        /// <param name="lineItemId">The line item ID to remove.</param>
        /// <returns>True if the item was removed, false otherwise.</returns>
        public bool RemoveLineItem(int lineItemId)
        {
            var item = LineItems.FirstOrDefault(li => li.Id == lineItemId);
            if (item != null)
            {
                LineItems.Remove(item);
                CalculateTotals();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the invoice totals based on line items.
        /// </summary>
        public void CalculateTotals()
        {
            SubTotal = LineItems.Sum(li => li.Amount);

            var taxableAmount = LineItems.Where(li => li.TaxApplicable).Sum(li => li.Amount);
            TaxAmount = taxableAmount * TaxRate;

            TotalAmount = SubTotal + TaxAmount - DiscountAmount;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Applies a discount to the invoice.
        /// </summary>
        /// <param name="discountAmount">The discount amount.</param>
        public void ApplyDiscount(decimal discountAmount)
        {
            if (discountAmount < 0)
                throw new ArgumentException("Discount amount cannot be negative", nameof(discountAmount));

            DiscountAmount = Math.Min(discountAmount, SubTotal);
            CalculateTotals();
        }

        /// <summary>
        /// Applies a payment to the invoice.
        /// </summary>
        /// <param name="paymentAmount">The payment amount.</param>
        public void ApplyPayment(decimal paymentAmount)
        {
            if (paymentAmount < 0)
                throw new ArgumentException("Payment amount cannot be negative", nameof(paymentAmount));

            if (paymentAmount > BalanceDue)
                throw new ArgumentException("Payment amount cannot exceed balance due", nameof(paymentAmount));

            PaidAmount += paymentAmount;

            // Update status based on payment
            if (BalanceDue == 0)
            {
                Status = InvoiceStatus.Paid;
            }
            else if (PaidAmount > 0)
            {
                Status = InvoiceStatus.PartiallyPaid;
            }

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the invoice is overdue.
        /// </summary>
        /// <returns>True if the invoice is overdue, false otherwise.</returns>
        public bool IsOverdue()
        {
            return DateTime.UtcNow.Date > DueDate.Date && BalanceDue > 0;
        }

        /// <summary>
        /// Gets the number of days overdue.
        /// </summary>
        /// <returns>The number of days overdue, or 0 if not overdue.</returns>
        public int DaysOverdue()
        {
            if (!IsOverdue())
                return 0;

            return (DateTime.UtcNow.Date - DueDate.Date).Days;
        }

        /// <summary>
        /// Calculates early payment discount if applicable.
        /// </summary>
        /// <returns>The early payment discount amount.</returns>
        public decimal CalculateEarlyPaymentDiscount()
        {
            if (Terms == null || Terms.EarlyPaymentDiscount <= 0 || Terms.EarlyPaymentDiscountDays <= 0)
                return 0;

            var discountDeadline = InvoiceDate.AddDays(Terms.EarlyPaymentDiscountDays);
            if (DateTime.UtcNow.Date <= discountDeadline.Date)
            {
                return TotalAmount * Terms.EarlyPaymentDiscount;
            }

            return 0;
        }

        /// <summary>
        /// Calculates late payment penalty if applicable.
        /// </summary>
        /// <returns>The late payment penalty amount.</returns>
        public decimal CalculateLatePenalty()
        {
            if (Terms == null || !IsOverdue() || Terms.LatePaymentPenalty <= 0)
                return 0;

            var penaltyStartDate = DueDate.AddDays(Terms.LatePaymentPenaltyDays);
            if (DateTime.UtcNow.Date > penaltyStartDate.Date)
            {
                return TotalAmount * Terms.LatePaymentPenalty;
            }

            return 0;
        }

        /// <summary>
        /// Validates the invoice data.
        /// </summary>
        /// <returns>True if the invoice is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(CustomerId) &&
                   !string.IsNullOrWhiteSpace(InvoiceNumber) &&
                   InvoiceDate != default &&
                   DueDate >= InvoiceDate &&
                   LineItems.Any() &&
                   TotalAmount >= 0 &&
                   PaidAmount >= 0 &&
                   PaidAmount <= TotalAmount;
        }
    }
}
