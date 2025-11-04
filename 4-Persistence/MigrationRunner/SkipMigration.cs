using System;
using Microsoft.Data.SqlClient;

class SkipMigration
{
    public static void MarkAsCompleted(long migrationVersion, string description)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("DB_CONNECTION_STRING environment variable not found.");
            return;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Check if migration already exists
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM [dbo].[VersionInfo] WHERE Version = @Version";
            checkCmd.Parameters.AddWithValue("@Version", migrationVersion);

            var exists = (int)checkCmd.ExecuteScalar() > 0;
            if (exists)
            {
                Console.WriteLine($"Migration {migrationVersion} already marked as completed.");
                return;
            }

            // Insert migration record to mark it as completed
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO [dbo].[VersionInfo] (Version, AppliedOn, Description)
                VALUES (@Version, @AppliedOn, @Description)";

            insertCmd.Parameters.AddWithValue("@Version", migrationVersion);
            insertCmd.Parameters.AddWithValue("@AppliedOn", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("@Description", description);

            insertCmd.ExecuteNonQuery();
            Console.WriteLine($"Successfully marked migration {migrationVersion} ({description}) as completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking migration as completed: {ex.Message}");
        }
    }
}
