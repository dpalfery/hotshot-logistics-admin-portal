#pragma warning disable SA1649
using System;
using System.Data;
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20251007030000)]
public class SeedDashboardTestData : Migration
{
    public override void Up()
    {
        // This migration updates existing seed data to create:
        // - 41 active jobs (status = 1, InProgress)
        // - 33 overdue invoices (due date in the past)
        // - This ensures the dashboard displays meaningful test data
        Execute.WithConnection((connection, transaction) =>
        {
            var rnd = new Random(54321); // deterministic randomness for dashboard data

            // Step 1: Update 41 jobs to InProgress status using a simpler approach
            using (var updateJobsCmd = connection.CreateCommand())
            {
                updateJobsCmd.Transaction = transaction;
                updateJobsCmd.CommandText = @"
                    UPDATE TOP (41) Jobs
                    SET Status = 1
                    WHERE Id LIKE 'job-cust-%' AND Status = 0";
                updateJobsCmd.ExecuteNonQuery();
            }

            // Step 2: Update 33 invoices to be overdue using a simpler approach
            using (var updateInvoicesCmd = connection.CreateCommand())
            {
                updateInvoicesCmd.Transaction = transaction;
                updateInvoicesCmd.CommandText = @"
                    UPDATE TOP (33) Invoices
                    SET DueDate = DATEADD(day, -60, GETDATE()),
                        Status = 5,  -- Overdue
                        PaidAmount = 0
                    WHERE Id LIKE 'inv-job-cust-%'";
                updateInvoicesCmd.ExecuteNonQuery();
            }
        });
    }

    public override void Down()
    {
        // Revert the changes by resetting status and dates
        Execute.WithConnection((connection, transaction) =>
        {
            // Reset jobs back to Pending status
            using var resetJobsCmd = connection.CreateCommand();
            resetJobsCmd.Transaction = transaction;
            resetJobsCmd.CommandText = @"
                UPDATE Jobs
                SET Status = 0
                WHERE Id LIKE 'job-cust-%' AND Status = 1";
            resetJobsCmd.ExecuteNonQuery();

            // Reset invoices back to original state
            using var resetInvoicesCmd = connection.CreateCommand();
            resetInvoicesCmd.Transaction = transaction;
            resetInvoicesCmd.CommandText = @"
                UPDATE Invoices
                SET DueDate = DATEADD(day, 30, InvoiceDate),
                    PaidAmount = 0,
                    Status = 0
                WHERE Id LIKE 'inv-job-cust-%'";
            resetInvoicesCmd.ExecuteNonQuery();
        });
    }

    /// <summary>
    /// Helper to create a parameter with null handling.
    /// </summary>
    /// <param name="cmd">The command to add the parameter to.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The created parameter.</returns>
    private static IDbDataParameter CreateParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        return p;
    }
}
