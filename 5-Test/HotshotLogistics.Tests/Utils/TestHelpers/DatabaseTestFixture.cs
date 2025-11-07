using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Tests;

/// <summary>
/// Shared test fixture that provisions the SQL Server database using the DbSetup CLI before integration tests run.
/// </summary>
public sealed class DatabaseTestFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim SetupSemaphore = new(1, 1);
    private static bool _initialized;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await SetupSemaphore.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            // Use the pre-configured connection string from environment to match application behavior
            var connectionString = TestDatabaseHelper.GetConnectionString();

            // Verify the database is reachable
            await VerifyConnectionAsync(connectionString);

            _initialized = true;
        }
        finally
        {
            SetupSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task RunDbSetupCliAsync(string solutionRoot, string server, string database, string appUser, string appPassword, string saConnectionString)
    {
        var projectPath = Path.Combine(solutionRoot, "7-Deployment", "DbSetup", "HotshotLogistics.DbSetup", "HotshotLogistics.DbSetup.csproj");
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException("DbSetup CLI project file was not found.", projectPath);
        }

        Console.WriteLine($"[TestSetup] Ensuring database '{database}' is provisioned via DbSetup CLI.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = solutionRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--non-interactive");
        startInfo.ArgumentList.Add("--server");
        startInfo.ArgumentList.Add(server);
        startInfo.ArgumentList.Add("--db-name");
        startInfo.ArgumentList.Add(database);
        startInfo.ArgumentList.Add("--app-user");
        startInfo.ArgumentList.Add(appUser);
        startInfo.ArgumentList.Add("--password");
        startInfo.ArgumentList.Add(appPassword);
        startInfo.ArgumentList.Add("--sa-connection-string");
        startInfo.ArgumentList.Add(saConnectionString);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start DbSetup CLI process.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        var waitTask = process.WaitForExitAsync();
        var completedTask = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMinutes(2)));
        if (completedTask != waitTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore clean-up failures
            }

            throw new TimeoutException("DbSetup CLI timed out while provisioning the integration test database.");
        }

        await waitTask;

        var output = await standardOutputTask;
        var error = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"DbSetup CLI exited with code {process.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{output}{Environment.NewLine}Error:{Environment.NewLine}{error}");
        }

        Console.WriteLine("[TestSetup] DbSetup CLI completed successfully.");
    }

    private static async Task VerifyConnectionAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT 1", connection);
        _ = await command.ExecuteScalarAsync();
    }

    private static string ResolveSolutionRoot()
    {
        const string solutionFile = "HotshotLogistics.sln";
        var directory = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, solutionFile)))
            {
                return directory;
            }

            var parent = Directory.GetParent(directory);
            if (parent is null)
            {
                break;
            }

            directory = parent.FullName;
        }

        throw new InvalidOperationException($"Unable to locate solution root containing '{solutionFile}'.");
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable '{name}' must be set for integration tests.");
        }

        return value;
    }
}
