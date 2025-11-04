#pragma warning disable SA1649
using System.Collections.Generic;
using FluentMigrator;
using HotshotLogistics.Core.Enums;
namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Updates job status distribution for better dashboard metrics testing.
/// </summary>
[Migration(20251009000000)]
public class UpdateJobStatusDistribution : Migration
{
    /// <summary>
    /// Applies the migration to update job status distribution.
    /// </summary>
    public override void Up()
    {
        // Update existing seed data to have more realistic status distribution
        Execute.WithConnection((connection, transaction) =>
        {
            var rnd = new Random(54321); // Different seed for variety

            // Update jobs to have better status distribution
            using var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = transaction;
            updateCmd.CommandText = @"
                UPDATE Jobs
                SET Status = @Status
                WHERE Id = @JobId";

            // Get all job IDs
            var jobIds = new List<string>();
            using (var selectCmd = connection.CreateCommand())
            {
                selectCmd.Transaction = transaction;
                selectCmd.CommandText = "SELECT Id FROM Jobs WHERE Id LIKE 'job-%'";
                using var reader = selectCmd.ExecuteReader();
                while (reader.Read())
                {
                    jobIds.Add(reader.GetString(0));
                }
            }

            // Distribute statuses: 40% Pending, 30% Assigned, 20% InProgress, 10% Completed
            var statusDistribution = new[]
            {
                (JobStatus.Pending, 0.40),
                (JobStatus.Assigned, 0.30),
                (JobStatus.InProgress, 0.20),
                (JobStatus.Completed, 0.10),
            };

            foreach (var jobId in jobIds)
            {
                var randomValue = rnd.NextDouble();
                var cumulativeProbability = 0.0;
                JobStatus selectedStatus = JobStatus.Pending;

                foreach (var (status, probability) in statusDistribution)
                {
                    cumulativeProbability += probability;
                    if (randomValue <= cumulativeProbability)
                    {
                        selectedStatus = status;
                        break;
                    }
                }

                updateCmd.Parameters.Clear();
                updateCmd.Parameters.Add(CreateParam(updateCmd, "@Status", (int)selectedStatus));
                updateCmd.Parameters.Add(CreateParam(updateCmd, "@JobId", jobId));
                updateCmd.ExecuteNonQuery();
            }
        });
    }

    /// <summary>
    /// Reverts the migration by resetting to original status distribution.
    /// </summary>
    public override void Down()
    {
        // Revert to original status distribution if needed
        Execute.WithConnection((connection, transaction) =>
        {
            using var revertCmd = connection.CreateCommand();
            revertCmd.Transaction = transaction;
            revertCmd.CommandText = @"
                UPDATE Jobs
                SET Status = (ABS(CHECKSUM(NEWID())) % 4) + 1
                WHERE Id LIKE 'job-%'";
            revertCmd.ExecuteNonQuery();
        });
    }

    private static System.Data.IDbDataParameter CreateParam(System.Data.IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        return p;
    }
}
