using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.DTOs;

/// <summary>
/// Represents the count of jobs for a specific status
/// </summary>
public class JobStatusCountDto
{
    public JobStatus Status { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Summary of job counts grouped by status
/// </summary>
public class JobStatusSummaryDto
{
    public int PendingCount { get; set; }
    public int AssignedCount { get; set; }
    public int EnRouteCount { get; set; }
    public int ReceivedCount { get; set; }
    public int TotalCount { get; set; }
}
