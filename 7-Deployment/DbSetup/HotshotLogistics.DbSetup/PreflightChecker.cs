using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.DbSetup;

public class PreflightChecker
{
    private readonly ILogger<PreflightChecker> _logger;

    public PreflightChecker(ILogger<PreflightChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> PerformPreflightChecksAsync(string connectionString, string databaseName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting preflight checks for database: {DatabaseName}", databaseName);

        var checks = new List<(string Name, Func<Task<bool>> Check)>
        {
            ("SQL Server Connectivity", () => CheckSqlServerConnectivityAsync(connectionString, cancellationToken)),
            ("Privileged Credentials", () => CheckPrivilegedCredentialsAsync(connectionString, cancellationToken)),
            ("Create Database Permission", () => CheckCreateDatabasePermissionAsync(connectionString, cancellationToken)),
            ("Create Login Permission", () => CheckCreateLoginPermissionAsync(connectionString, cancellationToken)),
            ("Database Access", () => CheckDatabaseAccessAsync(connectionString, databaseName, cancellationToken))
        };

        var allPassed = true;

        foreach (var (name, check) in checks)
        {
            try
            {
                _logger.LogDebug("Running preflight check: {CheckName}", name);
                var passed = await check();

                if (passed)
                {
                    _logger.LogInformation("✓ {CheckName} passed", name);
                }
                else
                {
                    _logger.LogError("✗ {CheckName} failed", name);
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ {CheckName} failed with exception", name);
                allPassed = false;
            }
        }

        if (allPassed)
        {
            _logger.LogInformation("All preflight checks passed");
        }
        else
        {
            _logger.LogError("Some preflight checks failed. Please review the errors above.");
        }

        return allPassed;
    }

    private async Task<bool> CheckSqlServerConnectivityAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Test with a simple query
            const string query = "SELECT SERVERPROPERTY('ProductVersion')";
            await using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            _logger.LogDebug("SQL Server connectivity test successful. Version: {Version}", result);
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError("SQL Server connectivity failed: {ErrorMessage}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SQL Server connectivity check");
            return false;
        }
    }

    private async Task<bool> CheckPrivilegedCredentialsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if we have sysadmin or sufficient privileges
            const string query = @"
                SELECT
                    IS_SRVROLEMEMBER('sysadmin') as IsSysAdmin,
                    IS_SRVROLEMEMBER('serveradmin') as IsServerAdmin,
                    HAS_PERMS_BY_NAME(null, null, 'CREATE ANY DATABASE') as CanCreateDatabase,
                    HAS_PERMS_BY_NAME(null, null, 'ALTER ANY LOGIN') as CanAlterLogin";

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var isSysAdmin = reader.GetBoolean(0);
                var isServerAdmin = reader.GetBoolean(1);
                var canCreateDatabase = reader.GetBoolean(2);
                var canAlterLogin = reader.GetBoolean(3);

                _logger.LogDebug("Privilege check results: SysAdmin={IsSysAdmin}, ServerAdmin={IsServerAdmin}, CanCreateDatabase={CanCreateDatabase}, CanAlterLogin={CanAlterLogin}",
                    isSysAdmin, isServerAdmin, canCreateDatabase, canAlterLogin);

                // We need either sysadmin, serveradmin, or specific permissions
                return isSysAdmin || isServerAdmin || (canCreateDatabase && canAlterLogin);
            }

            return false;
        }
        catch (SqlException ex)
        {
            _logger.LogError("Privileged credentials check failed: {ErrorMessage}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during privileged credentials check");
            return false;
        }
    }

    private async Task<bool> CheckCreateDatabasePermissionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Try to create a test database (we'll drop it immediately)
            var testDbName = $"TestDb_{Guid.NewGuid():N}";

            var createQuery = $"CREATE DATABASE {GetQuotedIdentifier(testDbName)}";
            await using (var command = new SqlCommand(createQuery, connection))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Clean up the test database
            var dropQuery = $"DROP DATABASE {GetQuotedIdentifier(testDbName)}";
            await using (var command = new SqlCommand(dropQuery, connection))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogDebug("Create database permission test successful");
            return true;
        }
        catch (SqlException ex) when (ex.Number == 262) // Database already exists
        {
            // This shouldn't happen with GUID, but handle it gracefully
            _logger.LogWarning("Test database already exists, skipping cleanup");
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError("Create database permission check failed: {ErrorMessage}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during create database permission check");
            return false;
        }
    }

    private async Task<bool> CheckCreateLoginPermissionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Try to create a test login (we'll drop it immediately)
            var testLoginName = $"TestLogin_{Guid.NewGuid():N}";
            var testPassword = "TempPassword123!";

            var createQuery = $"CREATE LOGIN {GetQuotedIdentifier(testLoginName)} WITH PASSWORD = @password";
            await using (var command = new SqlCommand(createQuery, connection))
            {
                command.Parameters.AddWithValue("@password", testPassword);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Clean up the test login
            var dropQuery = $"DROP LOGIN {GetQuotedIdentifier(testLoginName)}";
            await using (var command = new SqlCommand(dropQuery, connection))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogDebug("Create login permission test successful");
            return true;
        }
        catch (SqlException ex) when (ex.Number == 15025) // Login already exists
        {
            // This shouldn't happen with GUID, but handle it gracefully
            _logger.LogWarning("Test login already exists, skipping cleanup");
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError("Create login permission check failed: {ErrorMessage}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during create login permission check");
            return false;
        }
    }

    private async Task<bool> CheckDatabaseAccessAsync(string connectionString, string databaseName, CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = databaseName
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Test basic connectivity to the target database
            const string query = "SELECT DB_NAME()";
            await using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            var actualDbName = result?.ToString();
            if (actualDbName != databaseName)
            {
                _logger.LogWarning("Connected to database '{ActualDbName}' but expected '{ExpectedDbName}'", actualDbName, databaseName);
            }

            _logger.LogDebug("Database access test successful for database: {DatabaseName}", databaseName);
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError("Database access check failed for {DatabaseName}: {ErrorMessage}", databaseName, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during database access check for {DatabaseName}", databaseName);
            return false;
        }
    }

    public string GetRemediationInstructions()
    {
        return @"
To resolve privilege issues:

1. Ensure you're using SQL Server authentication with sufficient privileges
2. Use 'sa' account or an account with:
   - sysadmin role, OR
   - serveradmin role, OR
   - Both CREATE ANY DATABASE and ALTER ANY LOGIN permissions

3. For Windows authentication, ensure your Windows account has sufficient SQL Server privileges

4. In non-interactive mode, provide --sa-connection-string with privileged credentials

5. For Azure SQL Database, ensure the account has the necessary Azure RBAC roles

Example connection strings:
- Windows: Server=myserver;Database=master;Trusted_Connection=True;
- SQL Server: Server=myserver;Database=master;User Id=sa;Password=mypassword;
- Azure SQL: Server=myserver.database.windows.net;Database=master;User Id=myuser;Password=mypassword;
";
    }

    private static string GetQuotedIdentifier(string identifier)
    {
        return $"[{identifier.Replace("]", "]]")}]";
    }
}
