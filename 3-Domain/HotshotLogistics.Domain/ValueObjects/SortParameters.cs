
using HotshotLogistics.Core.Enums;
namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Parameters for sorting.
/// </summary>
public class SortParameters 
{
    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Descending;

    /// <summary>
    /// Gets or sets the sort direction (alternative name for compatibility).
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// Gets or sets additional sort fields for multi-column sorting.
    /// </summary>
    public List<SortField> AdditionalSorts { get; set; } = new List<SortField>();
}

/// <summary>
/// Represents a sort field with direction.
/// </summary>
public class SortField 
{
    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}
