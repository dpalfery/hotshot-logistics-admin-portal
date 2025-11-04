

namespace HotshotLogistics.Domain.ValueObjects
{
    /// <summary>
    /// Represents a line item on an invoice.
    /// </summary>
    public class InvoiceLineItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the line item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the description of the item or service.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets the total amount for this line item (calculated property).
        /// </summary>
        public decimal Amount => Quantity * UnitPrice;

        /// <summary>
        /// Gets or sets a value indicating whether tax applies to this item.
        /// </summary>
        public bool TaxApplicable { get; set; } = true;

        /// <summary>
        /// Gets or sets the sort order for display.
        /// </summary>
        public int SortOrder { get; set; }
    }
}
