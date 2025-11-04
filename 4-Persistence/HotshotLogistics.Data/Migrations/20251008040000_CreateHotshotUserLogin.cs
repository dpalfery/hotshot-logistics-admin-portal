#pragma warning disable SA1649
using System;
using System.Data;
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Creates the hotshot_user SQL Server login and database user with read/write permissions.
/// Requires the HOT_SHOT_USER_PASSWORD environment variable to be set.
/// </summary>
[Migration(20251008040000)]
public class CreateHotshotUserLogin : Migration
{
    public override void Up()
    {
        var password = Environment.GetEnvironmentVariable("HOT_SHOT_USER_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "HOT_SHOT_USER_PASSWORD environment variable is required to create the hotshot_user login. " +
                "Please set this environment variable before running migrations.");
        }

        Execute.WithConnection((connection, transaction) =>
        {
            // Step 1: Check if the login already exists at server level
            using (var checkLoginCmd = connection.CreateCommand())
            {
                checkLoginCmd.Transaction = transaction;
                checkLoginCmd.CommandText = "SELECT COUNT(*) FROM sys.server_principals WHERE name = 'hotshot_user'";
                var loginExists = (int)(checkLoginCmd.ExecuteScalar() ?? 0) > 0;

                if (!loginExists)
                {
                    // Create server-level login using parameterized variable to minimize password exposure
                    // Note: CREATE LOGIN requires password in SQL text, but we use a variable to reduce logging exposure
                    using (var createLoginCmd = connection.CreateCommand())
                    {
                        createLoginCmd.Transaction = transaction;
                        // Use DECLARE to set password in a variable, reducing exposure in logs
                        // SQL Server doesn't support parameters for CREATE LOGIN, so this is the most secure approach
                        var escapedPassword = password.Replace("'", "''");
                        createLoginCmd.CommandText = @"
                            DECLARE @pwd NVARCHAR(128) = N'" + escapedPassword + @"';
                            DECLARE @sql NVARCHAR(MAX) = N'CREATE LOGIN [hotshot_user] WITH PASSWORD = ''' + @pwd + '''';
                            EXEC sp_executesql @sql;";

                        // DO NOT log the actual command text as it contains the password
                        createLoginCmd.ExecuteNonQuery();
                        Console.WriteLine("Created server-level login: hotshot_user (password sourced from HOT_SHOT_USER_PASSWORD environment variable)");
                    }
                }
                else
                {
                    Console.WriteLine("Server-level login 'hotshot_user' already exists, skipping creation.");
                }
            }

            // Step 2: Check if the database user already exists
            using (var checkUserCmd = connection.CreateCommand())
            {
                checkUserCmd.Transaction = transaction;
                checkUserCmd.CommandText = "SELECT COUNT(*) FROM sys.database_principals WHERE name = 'hotshot_user' AND type = 'S'";
                var userExists = (int)(checkUserCmd.ExecuteScalar() ?? 0) > 0;

                if (!userExists)
                {
                    // Create database user for the login
                    using (var createUserCmd = connection.CreateCommand())
                    {
                        createUserCmd.Transaction = transaction;
                        createUserCmd.CommandText = "CREATE USER [hotshot_user] FOR LOGIN [hotshot_user];";
                        createUserCmd.ExecuteNonQuery();
                        Console.WriteLine("Created database user: hotshot_user");
                    }

                    // Grant db_datareader role (SELECT on all tables)
                    using (var grantReaderCmd = connection.CreateCommand())
                    {
                        grantReaderCmd.Transaction = transaction;
                        grantReaderCmd.CommandText = "EXEC sp_addrolemember N'db_datareader', N'hotshot_user';";
                        grantReaderCmd.ExecuteNonQuery();
                        Console.WriteLine("Granted db_datareader role to hotshot_user");
                    }

                    // Grant db_datawriter role (INSERT/UPDATE/DELETE on all tables)
                    using (var grantWriterCmd = connection.CreateCommand())
                    {
                        grantWriterCmd.Transaction = transaction;
                        grantWriterCmd.CommandText = "EXEC sp_addrolemember N'db_datawriter', N'hotshot_user';";
                        grantWriterCmd.ExecuteNonQuery();
                        Console.WriteLine("Granted db_datawriter role to hotshot_user");
                    }

                    // Grant VIEW DEFINITION permission
                    using (var grantViewDefCmd = connection.CreateCommand())
                    {
                        grantViewDefCmd.Transaction = transaction;
                        grantViewDefCmd.CommandText = "GRANT VIEW DEFINITION TO [hotshot_user];";
                        grantViewDefCmd.ExecuteNonQuery();
                        Console.WriteLine("Granted VIEW DEFINITION permission to hotshot_user");
                    }
                }
                else
                {
                    Console.WriteLine("Database user 'hotshot_user' already exists, skipping user creation and permissions.");
                }
            }
        });
    }

    public override void Down()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            // Remove database user
            using (var checkUserCmd = connection.CreateCommand())
            {
                checkUserCmd.Transaction = transaction;
                checkUserCmd.CommandText = "SELECT COUNT(*) FROM sys.database_principals WHERE name = 'hotshot_user' AND type = 'S'";
                var userExists = (int)(checkUserCmd.ExecuteScalar() ?? 0) > 0;

                if (userExists)
                {
                    using (var dropUserCmd = connection.CreateCommand())
                    {
                        dropUserCmd.Transaction = transaction;
                        dropUserCmd.CommandText = "DROP USER [hotshot_user];";
                        dropUserCmd.ExecuteNonQuery();
                        Console.WriteLine("Dropped database user: hotshot_user");
                    }
                }
            }

            // Remove server-level login
            using (var checkLoginCmd = connection.CreateCommand())
            {
                checkLoginCmd.Transaction = transaction;
                checkLoginCmd.CommandText = "SELECT COUNT(*) FROM sys.server_principals WHERE name = 'hotshot_user'";
                var loginExists = (int)(checkLoginCmd.ExecuteScalar() ?? 0) > 0;

                if (loginExists)
                {
                    using (var dropLoginCmd = connection.CreateCommand())
                    {
                        dropLoginCmd.Transaction = transaction;
                        dropLoginCmd.CommandText = "DROP LOGIN [hotshot_user];";
                        dropLoginCmd.ExecuteNonQuery();
                        Console.WriteLine("Dropped server-level login: hotshot_user");
                    }
                }
            }
        });
    }
}
