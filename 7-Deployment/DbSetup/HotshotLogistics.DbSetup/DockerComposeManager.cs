using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.DbSetup;

public class DockerComposeManager
{
    private readonly ILogger<DockerComposeManager> _logger;
    private readonly string _dockerComposeFilePath;

    public DockerComposeManager(ILogger<DockerComposeManager> logger, string dockerComposeFilePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockerComposeFilePath = dockerComposeFilePath ?? throw new ArgumentNullException(nameof(dockerComposeFilePath));

        if (!File.Exists(_dockerComposeFilePath))
        {
            throw new FileNotFoundException($"Docker compose file not found: {_dockerComposeFilePath}");
        }
    }

    public async Task<bool> IsDockerRunningAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start docker process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if Docker is running. Is Docker installed?");
            return false;
        }
    }

    public async Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"ps --filter \"name={containerName}\" --filter \"status=running\" --format \"{{{{.Names}}}}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return output.Contains(containerName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if container {ContainerName} is running", containerName);
            return false;
        }
    }

    public async Task<bool> StartContainerAsync(string saPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SQL Server container using docker-compose");
        Console.WriteLine();
        Console.WriteLine("Starting Docker container...");

        try
        {
            var workingDirectory = Path.GetDirectoryName(_dockerComposeFilePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = "up -d",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false  // Show the window
            };

            // Set the SA password environment variable
            startInfo.EnvironmentVariables["SQL_SA_PASSWORD"] = saPassword;

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start docker-compose process");
                return false;
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
                _logger.LogError("docker-compose up failed with exit code {ExitCode}", process.ExitCode);
                return false;
            }

            Console.WriteLine("✓ Docker container started successfully");
            _logger.LogInformation("Docker container started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Docker container");
            return false;
        }
    }

    public async Task<bool> StopContainerAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping SQL Server container using docker-compose");

        try
        {
            var workingDirectory = Path.GetDirectoryName(_dockerComposeFilePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = "down",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start docker-compose process");
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("docker-compose down failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("docker-compose output: {Output}", output);
            }

            _logger.LogInformation("Docker container stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop Docker container");
            return false;
        }
    }

    public async Task<bool> WaitForSqlServerReadyAsync(
        string connectionString,
        int maxRetries = 30,
        int delayMilliseconds = 2000,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Waiting for SQL Server to be ready (max {MaxRetries} retries)", maxRetries);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                const string query = "SELECT 1";
                await using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync(cancellationToken);

                if (result != null && (int)result == 1)
                {
                    _logger.LogInformation("SQL Server is ready (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _logger.LogDebug("SQL Server not ready yet (attempt {Attempt}/{MaxRetries}): {Message}",
                    i + 1, maxRetries, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unexpected error while waiting for SQL Server (attempt {Attempt}/{MaxRetries})",
                    i + 1, maxRetries);
            }

            if (i < maxRetries - 1)
            {
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }

        _logger.LogError("SQL Server failed to become ready after {MaxRetries} attempts", maxRetries);
        return false;
    }

    public string GetDockerConnectionString(string saPassword, string databaseName = "master")
    {
        return $"Server=localhost,1433;Database={databaseName};User Id=sa;Password={saPassword};TrustServerCertificate=true;";
    }
}
