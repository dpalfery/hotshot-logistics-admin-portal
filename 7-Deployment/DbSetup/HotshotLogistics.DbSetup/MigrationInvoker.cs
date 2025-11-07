using System.Diagnostics;
using System.Reflection;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.DbSetup;

public class MigrationInvoker
{
    private readonly ILogger<MigrationInvoker> _logger;

    public MigrationInvoker(ILogger<MigrationInvoker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> RunMigrationsAsync(string connectionString, string databaseName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting migration run for database: {DatabaseName}", databaseName);

        // First, try in-process migration if the assembly is available
        Assembly? migrationAssembly = null;
        try
        {
            migrationAssembly = Assembly.Load("HotshotLogistics.Data");
        }
        catch (FileNotFoundException)
        {
            _logger.LogInformation("HotshotLogistics.Data assembly not found in current context");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load HotshotLogistics.Data assembly");
        }

        if (migrationAssembly != null)
        {
            _logger.LogInformation("Found HotshotLogistics.Data assembly, running migrations in-process");
            return RunMigrationsInProcess(connectionString, migrationAssembly);
        }
        else
        {
            _logger.LogInformation("Falling back to subprocess migration runner");
            return await RunMigrationsAsSubprocessAsync(connectionString, cancellationToken);
        }
    }

    private bool RunMigrationsInProcess(string connectionString, Assembly migrationAssembly)
    {
        try
        {
            _logger.LogInformation("Running migrations in-process using FluentMigrator");

            var serviceProvider = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddSqlServer()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(migrationAssembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .BuildServiceProvider(false);

            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            // Check if there are any migrations to run
            var migrationInfos = runner.MigrationLoader.LoadMigrations();
            if (!migrationInfos.Any())
            {
                _logger.LogInformation("No migrations found to run");
                return true;
            }

            _logger.LogInformation("Found {MigrationCount} migrations to run", migrationInfos.Count());

            // Run the migrations
            runner.MigrateUp();

            _logger.LogInformation("Successfully completed all migrations in-process");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run migrations in-process");
            throw;
        }
    }

    private async Task<bool> RunMigrationsAsSubprocessAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running migrations as subprocess");
            Console.WriteLine();
            Console.WriteLine("Running database migrations...");

            // Find the MigrationRunner project
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = FindSolutionDirectory(currentDirectory);

            if (solutionDirectory == null)
            {
                throw new InvalidOperationException("Could not find solution directory");
            }

            var migrationRunnerPath = Path.Combine(
                solutionDirectory,
                "4-Persistence",
                "MigrationRunner",
                "bin",
                "net8.0",
                "MigrationRunner.dll"
            );

            if (!File.Exists(migrationRunnerPath))
            {
                // Try to build the MigrationRunner first
                Console.WriteLine("  Building MigrationRunner...");
                _logger.LogInformation("MigrationRunner.dll not found, attempting to build it");
                await BuildMigrationRunnerAsync(solutionDirectory, cancellationToken);

                if (!File.Exists(migrationRunnerPath))
                {
                    throw new FileNotFoundException($"MigrationRunner.dll not found at {migrationRunnerPath} after build attempt");
                }
            }

            // Run the migration runner as a subprocess
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{migrationRunnerPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,  // Show the window
            };
            
            // Pass connection string. If the caller didn't supply one, try several fallbacks:
            // 1) Environment variable "ConnectionStrings__DefaultConnection" (and common casing variants)
            // 2) Construct from individual environment variables (DB_SERVER, DB_PORT, DB_NAME, DB_APP_USER, HOTSHOT_DB_APP_PASSWORD, etc.)
            var finalConn = connectionString;
            // Track whether a ConnectionStrings__DefaultConnection value was present in the environment
            string? envConn = null;
            if (string.IsNullOrWhiteSpace(finalConn))
            {
                // Try canonical environment variable used by .NET configuration
                envConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                          ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION")
                          ?? Environment.GetEnvironmentVariable("connectionstrings__defaultconnection");

                finalConn = envConn;
            }

            if (string.IsNullOrWhiteSpace(finalConn))
            {
                // Attempt to construct from component environment variables
                var server = Environment.GetEnvironmentVariable("DB_SERVER")
                             ?? Environment.GetEnvironmentVariable("SERVER")
                             ?? Environment.GetEnvironmentVariable("HOTSHOT_DB_SERVER")
                             ?? Environment.GetEnvironmentVariable("HSL_DB_SERVER");

                var portStr = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("HOTSHOT_DB_PORT");
                int port = 1433;
                if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var parsedPort))
                {
                    port = parsedPort;
                }

                var database = Environment.GetEnvironmentVariable("DB_NAME")
                               ?? Environment.GetEnvironmentVariable("DATABASE")
                               ?? Environment.GetEnvironmentVariable("HOTSHOT_DB_NAME")
                               ?? Environment.GetEnvironmentVariable("HSL_DB_NAME");

                var user = Environment.GetEnvironmentVariable("DB_APP_USER")
                           ?? Environment.GetEnvironmentVariable("DB_USER")
                           ?? Environment.GetEnvironmentVariable("HOTSHOT_DB_APP_USER")
                           ?? Environment.GetEnvironmentVariable("HSL_DB_APP_USER");

                var password = Environment.GetEnvironmentVariable("HOTSHOT_DB_APP_PASSWORD")
                               ?? Environment.GetEnvironmentVariable("DB_APP_PASSWORD")
                               ?? Environment.GetEnvironmentVariable("DB_PASSWORD")
                               ?? Environment.GetEnvironmentVariable("HSL.DB_Password");

                if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(database) && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
                {
                    // Include port only when non-default
                    var serverPortSegment = port != 1433 ? $",{port}" : ",1433";
                    finalConn = $"Server={server}{serverPortSegment};Database={database};User Id={user};Password={password};TrustServerCertificate=true;";
                    _logger.LogInformation("Constructed DB connection string from environment components (password hidden)");

                    // If there was no existing ConnectionStrings__DefaultConnection env var, set the lowercase variant so downstream processes
                    // that check `connectionstrings__defaultconnection` will find it.
                    if (string.IsNullOrWhiteSpace(envConn))
                    {
                        try
                        {
                            // Persist the constructed connection string for the current user so downstream processes
                            // (and test infrastructure) can pick it up. Do NOT log or print the secret value.
                            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", finalConn, EnvironmentVariableTarget.User);
                            _logger.LogInformation("Persisted 'ConnectionStrings__DefaultConnection' in user environment (password hidden)");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to persist 'ConnectionStrings__DefaultConnection' to user environment");
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(finalConn))
            {
                _logger.LogWarning("DB connection string not provided and could not be resolved from environment variables. MigrationRunner requires DB_CONNECTION_STRING or ConnectionStrings__DefaultConnection.");
                // Still set value to empty so the subprocess will clearly error; we avoid throwing here to preserve existing behavior of subprocess invocation.
                startInfo.EnvironmentVariables["DB_CONNECTION_STRING"] = string.Empty;
            }
            else
            {
                startInfo.EnvironmentVariables["DB_CONNECTION_STRING"] = finalConn;
            }
            
            // Pass HOTSHOT_DB_APP_PASSWORD if it exists in the parent environment
            var hotshotDbAppPassword = Environment.GetEnvironmentVariable("HOTSHOT_DB_APP_PASSWORD");
            if (!string.IsNullOrEmpty(hotshotDbAppPassword))
            {
                startInfo.EnvironmentVariables["HOTSHOT_DB_APP_PASSWORD"] = hotshotDbAppPassword;
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start MigrationRunner process");
            }

            // Stream output in real-time
            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
            }, cancellationToken);

            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
            }, cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(outputTask, errorTask);

            Console.WriteLine();

            if (process.ExitCode != 0)
            {
                _logger.LogError("MigrationRunner subprocess failed with exit code {ExitCode}", process.ExitCode);
                throw new InvalidOperationException($"MigrationRunner failed with exit code {process.ExitCode}");
            }

            Console.WriteLine("✓ Migrations completed successfully");
            _logger.LogInformation("Successfully completed migrations via subprocess");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run migrations as subprocess");
            throw;
        }
    }

    private async Task BuildMigrationRunnerAsync(string solutionDirectory, CancellationToken cancellationToken = default)
    {
        var migrationRunnerProjectPath = Path.Combine(solutionDirectory, "4-Persistence", "MigrationRunner");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build --verbosity quiet",
            WorkingDirectory = migrationRunnerProjectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start build process for MigrationRunner");
        }

        // Stream build output
        var outputTask = Task.Run(async () =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine($"    {line}");
                }
            }
        }, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await outputTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to build MigrationRunner with exit code {process.ExitCode}");
        }

        Console.WriteLine("  ✓ MigrationRunner built successfully");
        _logger.LogInformation("Successfully built MigrationRunner project");
    }

    private static string? FindSolutionDirectory(string startDirectory)
    {
        var currentDirectory = new DirectoryInfo(startDirectory);

        while (currentDirectory != null)
        {
            var solutionFiles = currentDirectory.GetFiles("*.sln");
            if (solutionFiles.Any())
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}
