#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Updates existing job statuses to align with the new simplified JobStatus enum:
/// Pending, Assigned, EnRoute, Received
/// </summary>
[Migration(20251009060000)]
public class UpdateJobStatusesToNewEnum : Migration
{
    /// <summary>
    /// Applies the migration to update job statuses to new enum values.
    /// Maps old statuses to new ones:
    /// - Pending (0) -> Pending (0)
    /// - Assigned (1) -> Assigned (1)
    /// - EnRoute (2) -> EnRoute (2)
    /// - InProgress (3) -> EnRoute (2)
    /// - InTransit (4) -> EnRoute (2)
    /// - Completed (5) -> Received (3)
    /// - Delivered (6) -> Received (3)
    /// - Cancelled (7) -> Pending (0) [reset cancelled jobs to pending]
    /// </summary>
    public override void Up()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            using var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = transaction;

            // Map old statuses to new simplified ones
            updateCmd.CommandText = @"
                UPDATE Jobs
                SET Status = CASE
                    WHEN Status = 0 THEN 0  -- Pending -> Pending
                    WHEN Status = 1 THEN 1  -- Assigned -> Assigned
                    WHEN Status = 2 THEN 2  -- EnRoute -> EnRoute
                    WHEN Status = 3 THEN 2  -- InProgress -> EnRoute
                    WHEN Status = 4 THEN 2  -- InTransit -> EnRoute
                    WHEN Status = 5 THEN 3  -- Completed -> Received
                    WHEN Status = 6 THEN 3  -- Delivered -> Received
                    WHEN Status = 7 THEN 0  -- Cancelled -> Pending (reset)
                    ELSE 0  -- Default to Pending for any unexpected values
                END";

            updateCmd.ExecuteNonQuery();
        });
    }

    /// <summary>
    /// Reverts the migration. Note: This cannot perfectly restore original statuses
    /// since we've consolidated multiple statuses into fewer ones.
    /// </summary>
    public override void Down()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            using var revertCmd = connection.CreateCommand();
            revertCmd.Transaction = transaction;

            // Best effort reversion - map back to closest original statuses
            revertCmd.CommandText = @"
                UPDATE Jobs
                SET Status = CASE
                    WHEN Status = 0 THEN 0  -- Pending -> Pending
                    WHEN Status = 1 THEN 1  -- Assigned -> Assigned
                    WHEN Status = 2 THEN 2  -- EnRoute -> EnRoute (could have been InProgress or InTransit)
                    WHEN Status = 3 THEN 6  -- Received -> Delivered
                    ELSE 0  -- Default to Pending
                END";

            revertCmd.ExecuteNonQuery();
        });
    }
}
