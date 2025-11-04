namespace HotshotLogistics.Domain.DTOs;

/// <summary>
/// Parameters for pagination.
/// </summary>
public class PaginationParameters
{
    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page number (alternative name for compatibility).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum allowed page size.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Gets the validated page size (ensures it doesn't exceed MaxPageSize).
    /// </summary>
    public int ValidatedPageSize => Math.Min(PageSize, MaxPageSize);

    /// <summary>
    /// Gets the skip count for database queries.
    /// </summary>
    public int Skip => (Page - 1) * ValidatedPageSize;

    /// <summary>
    /// Gets the take count for database queries.
    /// </summary>
    public int Take => ValidatedPageSize;
}
