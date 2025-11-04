#pragma warning disable SA1649
using System.Collections.Generic;
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Sets custom job status distribution: 5 Pending, 3 Assigned, 8 En Route, 3 Received.
/// </summary>
[Migration(20251009010000)]
public class CustomJobStatusDistribution : Migration
{
    /// <summary>
    /// Applies the migration to set custom job status distribution.
    /// </summary>
    public override void Up()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            // Get all job IDs first
            var jobIds = new List<string>();
            using (var selectCmd = connection.CreateCommand())
            {
                selectCmd.Transaction = transaction;
                selectCmd.CommandText = "SELECT Id FROM Jobs WHERE Id LIKE 'job-%' ORDER BY Id";
                using var reader = selectCmd.ExecuteReader();
                while (reader.Read())
                {
                    jobIds.Add(reader.GetString(0));
                }
            }

            if (jobIds.Count < 19)
            {
                throw new InvalidOperationException($"Need at least 19 jobs for the desired distribution, but only found {jobIds.Count}");
            }

            // Set up the exact distribution: 5 Pending, 3 Assigned, 8 En Route, 3 Received
            var statusAssignments = new List<(string JobId, int Status)>();

            // 5 Pending (Status = 0)
            for (int i = 0; i < 5; i++)
            {
                statusAssignments.Add((jobIds[i], 0));
            }

            // 3 Assigned (Status = 1)
            for (int i = 5; i < 8; i++)
            {
                statusAssignments.Add((jobIds[i], 1));
            }

            // 8 En Route (Status = 2)
            for (int i = 8; i < 16; i++)
            {
                statusAssignments.Add((jobIds[i], 2));
            }

            // 3 Received (Status = 3)
            for (int i = 16; i < 19; i++)
            {
                statusAssignments.Add((jobIds[i], 3));
            }

            // Set remaining jobs to legacy Completed status to keep them out of active counts
            for (int i = 19; i < jobIds.Count; i++)
            {
                statusAssignments.Add((jobIds[i], 6)); // Legacy Completed
            }

            //Apply the status assignments
            using var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = transaction;
            updateCmd.CommandText = "UPDATE Jobs SET Status = @Status WHERE Id = @JobId";

            foreach (var (jobId, status) in statusAssignments)
            {
                updateCmd.Parameters.Clear();
                updateCmd.Parameters.Add(CreateParam(updateCmd, "@Status", status));
                updateCmd.Parameters.Add(CreateParam(updateCmd, "@JobId", jobId));
                updateCmd.ExecuteNonQuery();
            }
        });
    }

    /// <summary>
    /// Reverts the migration by resetting to random status distribution.
    /// </summary>
    public override void Down()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            using var revertCmd = connection.CreateCommand();
            revertCmd.Transaction = transaction;
            revertCmd.CommandText = @"
                UPDATE Jobs
                SET Status = (ABS(CHECKSUM(NEWID())) % 4)
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
