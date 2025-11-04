#pragma warning disable SA1649
using System.Collections.Generic;
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Seeds the Jobs table with sample data ensuring at least 5 instances
/// of each new JobStatus: Pending, Assigned, EnRoute, Received
/// </summary>
[Migration(20251009060100)]
public class SeedJobsWithNewStatuses : Migration
{
    /// <summary>
    /// Applies the migration to seed jobs with new status distribution.
    /// </summary>
    public override void Up()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            // First, check if we have enough jobs with each status
            using var checkCmd = connection.CreateCommand();
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = @"
                SELECT Status, COUNT(*) as Count
                FROM Jobs
                WHERE Status IN (0, 1, 2, 3)
                GROUP BY Status";

            var statusCounts = new Dictionary<int, int>();
            using (var reader = checkCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    statusCounts[reader.GetInt32(0)] = reader.GetInt32(1);
                }
            }

            // Get existing customer IDs for reference
            var customerIds = new List<string>();
            using (var customerCmd = connection.CreateCommand())
            {
                customerCmd.Transaction = transaction;
                customerCmd.CommandText = "SELECT TOP 5 Id FROM Customers ORDER BY Id";
                using var customerReader = customerCmd.ExecuteReader();
                while (customerReader.Read())
                {
                    customerIds.Add(customerReader.GetString(0));
                }
            }

            // If we don't have any customers, create a default one
            if (customerIds.Count == 0)
            {
                using var createCustomerCmd = connection.CreateCommand();
                createCustomerCmd.Transaction = transaction;
                createCustomerCmd.CommandText = @"
                    INSERT INTO Customers (Id, Name, Address, City, State, ZipCode, CreatedAt)
                    VALUES ('cust-seed-001', 'Seed Customer', '123 Main St', 'Test City', 'TX', '12345', @CreatedAt)";

                var customerParam = createCustomerCmd.CreateParameter();
                customerParam.ParameterName = "@CreatedAt";
                customerParam.Value = DateTime.UtcNow;
                createCustomerCmd.Parameters.Add(customerParam);
                createCustomerCmd.ExecuteNonQuery();

                customerIds.Add("cust-seed-001");
            }

            // Create jobs to ensure we have at least 5 of each status
            var jobsToCreate = new List<(string JobId, string CustomerId, string Title, int Status, string Description)>();

            // Pending jobs (Status = 0)
            var pendingCount = statusCounts.GetValueOrDefault(0, 0);
            for (int i = pendingCount; i < 5; i++)
            {
                jobsToCreate.Add((
                    $"job-pending-seed-{i + 1:D3}",
                    customerIds[i % customerIds.Count],
                    $"Pending Job {i + 1}",
                    0, // Pending
                    "Awaiting assignment to driver"
                ));
            }

            // Assigned jobs (Status = 1)
            var assignedCount = statusCounts.GetValueOrDefault(1, 0);
            for (int i = assignedCount; i < 5; i++)
            {
                jobsToCreate.Add((
                    $"job-assigned-seed-{i + 1:D3}",
                    customerIds[i % customerIds.Count],
                    $"Assigned Job {i + 1}",
                    1, // Assigned
                    "Assigned to driver, awaiting pickup"
                ));
            }

            // EnRoute jobs (Status = 2)
            var enRouteCount = statusCounts.GetValueOrDefault(2, 0);
            for (int i = enRouteCount; i < 5; i++)
            {
                jobsToCreate.Add((
                    $"job-enroute-seed-{i + 1:D3}",
                    customerIds[i % customerIds.Count],
                    $"En Route Job {i + 1}",
                    2, // EnRoute
                    "Driver en route to pickup or delivery location"
                ));
            }

            // Received jobs (Status = 3)
            var receivedCount = statusCounts.GetValueOrDefault(3, 0);
            for (int i = receivedCount; i < 5; i++)
            {
                jobsToCreate.Add((
                    $"job-received-seed-{i + 1:D3}",
                    customerIds[i % customerIds.Count],
                    $"Received Job {i + 1}",
                    3, // Received
                    "Cargo delivered and received by customer"
                ));
            }

            // Insert the new jobs
            using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT INTO Jobs (
                    Id, CustomerId, Title, PickupAddress, DeliveryAddress,
                    CargoDescription, Status, Priority, BaseRate, MileageRate,
                    TotalAmount, EstimatedDeliveryTime, SpecialInstructions,
                    CreatedAt, UpdatedAt
                ) VALUES (
                    @Id, @CustomerId, @Title, @PickupAddress, @DeliveryAddress,
                    @CargoDescription, @Status, @Priority, @BaseRate, @MileageRate,
                    @TotalAmount, @EstimatedDeliveryTime, @SpecialInstructions,
                    @CreatedAt, @UpdatedAt
                )";

            foreach (var (jobId, customerId, title, status, description) in jobsToCreate)
            {
                insertCmd.Parameters.Clear();

                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Id", jobId));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CustomerId", customerId));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Title", title));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@PickupAddress", "123 Pickup St, Test City, TX 12345"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@DeliveryAddress", "456 Delivery Ave, Test City, TX 12345"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CargoDescription", description));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Status", status));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Priority", 2)); // Medium priority
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@BaseRate", 150.00m));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@MileageRate", 2.50m));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@TotalAmount", 200.00m));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@EstimatedDeliveryTime", DateTime.UtcNow.AddDays(1)));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@SpecialInstructions", $"Special instructions for {title.ToLower()}"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CreatedAt", DateTime.UtcNow));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@UpdatedAt", DateTime.UtcNow));

                insertCmd.ExecuteNonQuery();
            }
        });
    }

    /// <summary>
    /// Reverts the migration by removing seeded jobs.
    /// </summary>
    public override void Down()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = @"
                DELETE FROM Jobs
                WHERE Id LIKE 'job-%-seed-%'";
            deleteCmd.ExecuteNonQuery();

            // Also remove the seed customer if created
            using var deleteCustomerCmd = connection.CreateCommand();
            deleteCustomerCmd.Transaction = transaction;
            deleteCustomerCmd.CommandText = @"
                DELETE FROM Customers
                WHERE Id = 'cust-seed-001'";
            deleteCustomerCmd.ExecuteNonQuery();
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
