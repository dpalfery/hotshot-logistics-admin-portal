using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Tests
{
    /// <summary>
    /// Helper class for building test database connection strings from environment variables.
    /// </summary>
    public static class TestDatabaseHelper
    {
        private static readonly object SyncLock = new();
        private static string? cachedConnectionString;

        /// <summary>
        /// Gets the SQL Server connection string from the environment variable used by the app repos.
        /// </summary>
        /// <returns>A valid SQL Server connection string.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the required environment variable is not set.</exception>
        public static string GetConnectionString()
        {
            if (cachedConnectionString is not null)
            {
                return cachedConnectionString;
            }

            lock (SyncLock)
            {
                if (cachedConnectionString is not null)
                {
                    return cachedConnectionString;
                }

                // Read the connection string from environment to match application configuration
                var conn = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
                if (string.IsNullOrWhiteSpace(conn))
                {
                    throw new InvalidOperationException("Environment variable 'CONNECTIONSTRINGS__DEFAULTCONNECTION' is required for tests");
                }

                cachedConnectionString = conn;
                return cachedConnectionString;
            }
        }

        private static IEnumerable<SqlConnectionStringBuilder> BuildCredentialBuilders(string server)
        {
            var builders = new List<SqlConnectionStringBuilder>();

            var saPassword = Environment.GetEnvironmentVariable("SQL_SA_PASSWORD");
            if (!string.IsNullOrWhiteSpace(saPassword))
            {
                builders.Add(new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    UserID = "sa",
                    Password = saPassword,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                });
            }

            var saConnectionString = Environment.GetEnvironmentVariable("CI_SA_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(saConnectionString))
            {
                var builder = new SqlConnectionStringBuilder(saConnectionString)
                {
                    DataSource = server,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                };

                builders.Add(builder);
            }

            var appUser = Environment.GetEnvironmentVariable("HOTSHOT_DB_APP_USER");
            var appPassword = Environment.GetEnvironmentVariable("HOTSHOT_DB_PASSWORD");
            if (!string.IsNullOrWhiteSpace(appUser) && !string.IsNullOrWhiteSpace(appPassword))
            {
                builders.Add(new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    UserID = appUser,
                    Password = appPassword,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                });
            }

            return builders;
        }

        private static IEnumerable<string> BuildCandidateDatabases(string? baseDatabaseName)
        {
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(baseDatabaseName))
            {
                candidates.Add(baseDatabaseName);

                if (!baseDatabaseName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add($"{baseDatabaseName}Test");
                    candidates.Add($"{baseDatabaseName}_Test");
                }
            }

            candidates.Add("HotshotLogistics");
            candidates.Add("HotshotLogisticsTest");
            candidates.Add("hotshot_logistics");
            candidates.Add("hotshot_logistics_test");

            return candidates
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryOpenConnection(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                return true;
            }
            catch (SqlException ex) when (IsAuthenticationOrDatabaseError(ex))
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool IsAuthenticationOrDatabaseError(SqlException exception)
        {
            foreach (SqlError error in exception.Errors)
            {
                if (error.Number == 18456 || error.Number == 4060)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetRequiredEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{name} environment variable is required for tests");
            }

            return value;
        }
    }
}
