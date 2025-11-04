// <copyright file="PricingDetails.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.ValueObjects
{
    using System;

    /// <summary>
    /// Represents pricing details for a job.
    /// </summary>
    public class PricingDetails
    {
        /// <summary>
        /// Gets or sets the base rate for the job.
        /// </summary>
        public decimal BaseRate { get; set; }

        /// <summary>
        /// Gets or sets the rate per mile.
        /// </summary>
        public decimal MileageRate { get; set; }

        /// <summary>
        /// Gets or sets the fuel surcharge amount.
        /// </summary>
        public decimal FuelSurcharge { get; set; }

        /// <summary>
        /// Gets or sets the toll charges.
        /// </summary>
        public decimal TollCharges { get; set; }

        /// <summary>
        /// Gets or sets any additional charges.
        /// </summary>
        public decimal AdditionalCharges { get; set; }

        /// <summary>
        /// Gets or sets the total amount for the job.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the currency code (default: USD).
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Gets or sets any discount applied.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Gets or sets the tax amount.
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Gets or sets the tax rate as a percentage.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Gets or sets notes about the pricing.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Gets the subtotal before tax and discount.
        /// </summary>
        public decimal Subtotal => BaseRate + FuelSurcharge + TollCharges + AdditionalCharges;

        /// <summary>
        /// Gets the net amount after discount and tax.
        /// </summary>
        public decimal NetAmount => Subtotal - Discount + Tax;

        /// <summary>
        /// Calculates the total pricing based on distance.
        /// </summary>
        /// <param name="distance">The distance in miles.</param>
        /// <param name="applyTax">Whether to apply tax.</param>
        public void CalculateTotal(decimal distance, bool applyTax = true)
        {
            var mileageCharges = MileageRate * distance;
            var subtotal = BaseRate + mileageCharges + FuelSurcharge + TollCharges + AdditionalCharges;

            var discountedAmount = subtotal - Discount;

            if (applyTax && TaxRate > 0)
            {
                Tax = discountedAmount * (TaxRate / 100);
            }

            TotalAmount = discountedAmount + Tax;
        }

        /// <summary>
        /// Applies a discount to the pricing.
        /// </summary>
        /// <param name="discountAmount">The discount amount.</param>
        /// <param name="recalculate">Whether to recalculate the total.</param>
        public void ApplyDiscount(decimal discountAmount, bool recalculate = true)
        {
            Discount = Math.Max(0, discountAmount);

            if (recalculate)
            {
                var subtotal = BaseRate + FuelSurcharge + TollCharges + AdditionalCharges;
                var discountedAmount = subtotal - Discount;

                if (TaxRate > 0)
                {
                    Tax = discountedAmount * (TaxRate / 100);
                }

                TotalAmount = discountedAmount + Tax;
            }
        }

        /// <summary>
        /// Validates the pricing details.
        /// </summary>
        /// <returns>True if the pricing is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return BaseRate >= 0 &&
                   MileageRate >= 0 &&
                   FuelSurcharge >= 0 &&
                   TollCharges >= 0 &&
                   AdditionalCharges >= 0 &&
                   TotalAmount >= 0 &&
                   Discount >= 0 &&
                   Tax >= 0 &&
                   TaxRate >= 0 &&
                   TaxRate <= 100;
        }
    }
}
