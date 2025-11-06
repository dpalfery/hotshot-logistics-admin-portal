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
            
            // Pass connection string
            startInfo.EnvironmentVariables["DB_CONNECTION_STRING"] = connectionString;
            
            // Pass HOT_SHOT_USER_PASSWORD if it exists in the parent environment
            var hotshotUserPassword = Environment.GetEnvironmentVariable("HOT_SHOT_USER_PASSWORD");
            if (!string.IsNullOrEmpty(hotshotUserPassword))
            {
                startInfo.EnvironmentVariables["HOT_SHOT_USER_PASSWORD"] = hotshotUserPassword;
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
