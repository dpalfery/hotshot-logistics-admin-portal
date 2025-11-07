using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HotshotLogistics.DbSetup;

namespace HotshotLogistics.DbSetup
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<Program>();
            var envManagerLogger = loggerFactory.CreateLogger<EnvironmentManager>();
            var environmentManager = new EnvironmentManager(envManagerLogger);

            var passwordManagerLogger = loggerFactory.CreateLogger<PasswordManager>();
            var passwordManager = new PasswordManager(passwordManagerLogger);

            var provisionerLogger = loggerFactory.CreateLogger<SqlServerProvisioner>();
            var provisioner = new SqlServerProvisioner(provisionerLogger);

<<<<<<< HEAD
=======
            var preflightLogger = loggerFactory.CreateLogger<PreflightChecker>();
            var preflightChecker = new PreflightChecker(preflightLogger);

            var migrationLogger = loggerFactory.CreateLogger<MigrationInvoker>();
            var migrationInvoker = new MigrationInvoker(migrationLogger);

>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
            try
            {
                // Parse command line arguments
                var commandLineArgs = args ?? Array.Empty<string>();
                var parser = new ArgumentParser(commandLineArgs);
                parser.ApplyEnvironmentOverrides();

                if (!parser.Validate(out var errorMessage))
                {
                    logger.LogError("Validation failed: {ErrorMessage}", errorMessage);
                    Environment.Exit(1);
                }

<<<<<<< HEAD
                logger.LogInformation("Starting Hotshot Logistics Database Setup");
                logger.LogInformation("Server: {Server}", parser.Server);
=======
                logger.LogInformation("Starting Database Setup");
                logger.LogInformation("Project Slug: {ProjectSlug}", parser.ProjectSlug);
                logger.LogInformation("Docker Mode: {UseDocker}", parser.UseDocker);

                // Create interactive prompter
                var prompterLogger = loggerFactory.CreateLogger<InteractivePrompter>();
                var prompter = new InteractivePrompter(prompterLogger);

                // Prompt for missing required values in interactive mode
                if (!parser.NonInteractive)
                {
                    // Prompt for SA password if using Docker
                    if (parser.UseDocker && string.IsNullOrEmpty(parser.SaPassword))
                    {
                        Console.WriteLine();
                        parser.SaPassword = prompter.PromptForPassword("Enter SA password for SQL Server (min 8 chars, mixed case, numbers, symbols)", confirm: false);
                    }

                    // Prompt for server
                    if (string.IsNullOrEmpty(parser.Server))
                    {
                        var defaultServer = parser.UseDocker ? "localhost" : "localhost\\SQLEXPRESS";
                        parser.Server = prompter.PromptForInput("SQL Server instance", defaultServer, required: true);
                    }

                    // Prompt for database name
                    if (string.IsNullOrEmpty(parser.DatabaseName))
                    {
                        var defaultDbName = $"{parser.ProjectSlug}_db";
                        parser.DatabaseName = prompter.PromptForInput("Database name", defaultDbName, required: true);
                    }

                    // Prompt for app user
                    if (string.IsNullOrEmpty(parser.AppUser))
                    {
                        var defaultAppUser = $"{parser.ProjectSlug}_app";
                        parser.AppUser = prompter.PromptForInput("Application user name", defaultAppUser, required: true);
                    }
                }
                else
                {
                    // Non-interactive mode: require all values or use defaults
                    if (parser.UseDocker && string.IsNullOrEmpty(parser.SaPassword))
                    {
                        logger.LogError("SA password is required in non-interactive Docker mode");
                        Environment.Exit(1);
                    }

                    parser.Server ??= parser.UseDocker ? "localhost" : throw new InvalidOperationException("Server is required in non-interactive mode");
                    parser.DatabaseName ??= $"{parser.ProjectSlug}_db";
                    parser.AppUser ??= $"{parser.ProjectSlug}_app";
                }

                logger.LogInformation("Server: {Server}:{Port}", parser.Server, parser.Port);
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
                logger.LogInformation("Database: {Database}", parser.DatabaseName);
                logger.LogInformation("App User: {AppUser}", parser.AppUser);

                // Check if running with administrator privileges
                if (environmentManager.IsAdministrator())
                {
                    logger.LogInformation("Running with administrator privileges");
                }
                else
                {
                    logger.LogWarning("Not running with administrator privileges. Some operations may fail.");
                }

<<<<<<< HEAD
                // Build SA connection string
                // Preference order:
                // 1) --sa-connection-string (CLI)
                // 2) Build from HOTSHOT_DB_* environment variables (HOTSHOT_DB_SA_USER, HOTSHOT_DB_SA_PASSWORD)
                // 3) fallback to Integrated Security (may cause SSPI/Kerberos issues)
                string saConnectionString;
                if (!string.IsNullOrWhiteSpace(parser.SaConnectionString))
                {
                    saConnectionString = parser.SaConnectionString;
                    logger.LogInformation("Using supplied SA connection string (password hidden)");
                }
                else
                {
                    // Read SA user/password from the app's HOTSHOT_DB_* env vars (preferred)
                    var envSaPassword = Environment.GetEnvironmentVariable("HOTSHOT_DB_SA_PASSWORD")
                                       ?? Environment.GetEnvironmentVariable("SQL_SA_PASSWORD")
                                       ?? Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");

                    var envSaUser = Environment.GetEnvironmentVariable("HOTSHOT_DB_SA_USER") ?? "sa";

                    if (!string.IsNullOrWhiteSpace(envSaPassword))
                    {
                        saConnectionString = $"Server={parser.Server};Database=master;User Id={envSaUser};Password={envSaPassword};TrustServerCertificate=true;";
                        logger.LogInformation("Built SA connection string from HOTSHOT_DB_* environment variables (password hidden)");
                    }
                    else
                    {
                        saConnectionString = $"Server={parser.Server};Database=master;Integrated Security=true;TrustServerCertificate=true;";
                        logger.LogWarning("No SA credentials supplied. Falling back to Integrated Security which may fail with SSPI/Kerberos errors.");
                    }
                }

                logger.LogInformation("Using connection string for provisioning (password hidden)");

                // Generate or get password for app user
                var appPassword = parser.Password ?? passwordManager.GeneratePassword();
                logger.LogInformation("Generated password for application user (password hidden)");
=======
                // Handle Docker mode
                if (parser.UseDocker)
                {
                    logger.LogInformation("Starting Docker container setup...");

                    var dockerLogger = loggerFactory.CreateLogger<DockerComposeManager>();
                    var dockerManager = new DockerComposeManager(dockerLogger, parser.DockerComposeFile!);

                    // Check if Docker is running
                    var dockerRunning = await dockerManager.IsDockerRunningAsync();
                    if (!dockerRunning)
                    {
                        logger.LogError("Docker is not running. Please start Docker Desktop and try again.");
                        Environment.Exit(1);
                    }

                    logger.LogInformation("Docker is running");

                    // Check if container is already running
                    var containerRunning = await dockerManager.IsContainerRunningAsync("hotshot_sqlserver");
                    if (!containerRunning)
                    {
                        logger.LogInformation("SQL Server container is not running. Starting container...");
                        var started = await dockerManager.StartContainerAsync(parser.SaPassword!);
                        if (!started)
                        {
                            logger.LogError("Failed to start SQL Server container");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        logger.LogInformation("SQL Server container is already running");
                    }

                    // Wait for SQL Server to be ready
                    var dockerConnectionString = dockerManager.GetDockerConnectionString(parser.SaPassword!);
                    var sqlReady = await dockerManager.WaitForSqlServerReadyAsync(dockerConnectionString);
                    if (!sqlReady)
                    {
                        logger.LogError("SQL Server failed to become ready in time");
                        Environment.Exit(1);
                    }
                }

                // Build SA connection string from components
                var saConnectionString = parser.UseDocker
                    ? $"Server={parser.Server},{parser.Port};Database=master;User Id=sa;Password={parser.SaPassword};TrustServerCertificate=true;"
                    : BuildConnectionString(parser.Server!, parser.Port, "master", saPassword: parser.SaPassword);

                logger.LogInformation("Using connection string for provisioning (password hidden)");

                // Run preflight checks
                logger.LogInformation("Running preflight checks...");
                var preflightPassed = await preflightChecker.PerformPreflightChecksAsync(saConnectionString, parser.DatabaseName!, CancellationToken.None);
                if (!preflightPassed)
                {
                    logger.LogError("Preflight checks failed. Please review the errors above.");
                    logger.LogInformation(preflightChecker.GetRemediationInstructions());
                    Environment.Exit(1);
                }

                // Generate or get password for app user
                var appPassword = parser.AppPassword ?? passwordManager.GeneratePassword();
                if (string.IsNullOrEmpty(parser.AppPassword))
                {
                    logger.LogInformation("Generated password for application user (password hidden)");
                }
                else
                {
                    logger.LogInformation("Using provided password for application user");
                }
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb

                // Provision the database
                logger.LogInformation("Starting database provisioning...");
                var success = await provisioner.ProvisionDatabaseAsync(
                    saConnectionString,
                    parser.DatabaseName!,
                    parser.AppUser!,
                    appPassword);

                if (!success)
                {
                    logger.LogError("Database provisioning failed");
                    Environment.Exit(1);
                }

<<<<<<< HEAD
                // Build application connection string (keep as local value only)
                var appConnectionString = $"Server={parser.Server};Database={parser.DatabaseName};User Id={parser.AppUser};Password={appPassword};TrustServerCertificate=true;";

                // Persist connection string & password with user consent if requested
                if (parser.PersistEnvironment)
                {
                    // Persist HOTSHOT_DB_PASSWORD separately and the canonical ConnectionStrings key
                    environmentManager.PersistWithConsent("HOTSHOT_DB_PASSWORD", appPassword, parser.NonInteractive);
                    environmentManager.PersistWithConsent("CONNECTIONSTRINGS__DEFAULTCONNECTION", appConnectionString, parser.NonInteractive);
                }
                else if (!parser.NonInteractive)
                {
                    // Interactive mode but not persisting: show masked info and instructions without revealing secrets
                    Console.WriteLine();
                    Console.WriteLine("Connection string generated (password is hidden for security)");
                    Console.WriteLine($"  Server: {parser.Server}");
                    Console.WriteLine($"  Database: {parser.DatabaseName}");
                    Console.WriteLine($"  User: {parser.AppUser}");
                    Console.WriteLine();
                    Console.WriteLine("To set the connection string as an environment variable, run:");
                    Console.WriteLine("  export CONNECTIONSTRINGS__DEFAULTCONNECTION=\"Server=<server>;Database=<db>;User Id=<user>;Password=<password>;TrustServerCertificate=true;\"");
                    Console.WriteLine("Or on Windows PowerShell:");
                    Console.WriteLine("  [Environment]::SetEnvironmentVariable(\"CONNECTIONSTRINGS__DEFAULTCONNECTION\", \"Server=<server>;Database=<db>;User Id=<user>;Password=<password>;TrustServerCertificate=true;\", 'User')");
                    Console.WriteLine();
                    Console.WriteLine("Note: Do NOT paste the password into shared logs or commit it to source control.");
                }

                // TODO: Implement the rest of the components

                // - PermissionManager (if additional permissions needed beyond basic DML)
                // - MigrationInvoker (to run FluentMigrator migrations)

                logger.LogInformation("Database setup completed successfully!");
                Console.WriteLine();
                Console.WriteLine("✓ Database setup completed successfully!");
=======
                // Build application connection string
                var appConnectionString = $"Server={parser.Server},{parser.Port};Database={parser.DatabaseName};User Id={parser.AppUser};Password={appPassword};TrustServerCertificate=true;";

                // Run migrations using SA connection (needs elevated permissions)
                logger.LogInformation("Running database migrations...");
                var migrationConnectionString = $"Server={parser.Server},{parser.Port};Database={parser.DatabaseName};User Id=sa;Password={parser.SaPassword};TrustServerCertificate=true;";
                var migrationsSuccess = await migrationInvoker.RunMigrationsAsync(migrationConnectionString, parser.DatabaseName!, CancellationToken.None);
                if (!migrationsSuccess)
                {
                    logger.LogError("Failed to run database migrations");
                    Environment.Exit(1);
                }

                // Persist environment variables
                Console.WriteLine();
                Console.WriteLine("Setting environment variables...");

                var envPrefix = parser.ProjectSlug!.ToUpperInvariant();
                try
                {
                    environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_SERVER", parser.Server!, EnvironmentVariableTarget.User);
                    Console.WriteLine($"  ✓ {envPrefix}_DB_SERVER");
                    
                    environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_PORT", parser.Port.ToString(), EnvironmentVariableTarget.User);
                    Console.WriteLine($"  ✓ {envPrefix}_DB_PORT");
                    
                    environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_NAME", parser.DatabaseName!, EnvironmentVariableTarget.User);
                    Console.WriteLine($"  ✓ {envPrefix}_DB_NAME");
                    
                    environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_APP_USER", parser.AppUser!, EnvironmentVariableTarget.User);
                    Console.WriteLine($"  ✓ {envPrefix}_DB_APP_USER");
                    
                    environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_APP_PASSWORD", appPassword, EnvironmentVariableTarget.User);
                    Console.WriteLine($"  ✓ {envPrefix}_DB_APP_PASSWORD");

                    if (parser.UseDocker)
                    {
                        environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_SA_PASSWORD", parser.SaPassword!, EnvironmentVariableTarget.User);
                        Console.WriteLine($"  ✓ {envPrefix}_DB_SA_PASSWORD");
                        
                        environmentManager.SetEnvironmentVariable($"{envPrefix}_DB_USE_DOCKER", "true", EnvironmentVariableTarget.User);
                        Console.WriteLine($"  ✓ {envPrefix}_DB_USE_DOCKER");
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("✓ All environment variables set successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to set environment variables");
                    Console.WriteLine();
                    Console.WriteLine($"✗ Failed to set environment variables: {ex.Message}");
                    Console.WriteLine("You may need to run as administrator or set them manually.");
                }

                // Display summary
                if (!parser.NonInteractive)
                {
                    Console.WriteLine();
                    Console.WriteLine("✓ Setup complete!");
                    Console.WriteLine();
                    Console.WriteLine("Connection string:");
                    Console.WriteLine($"  {appConnectionString}");
                    Console.WriteLine();
                    Console.WriteLine($"Environment variables set (using project slug '{envPrefix}'):");
                    Console.WriteLine($"  {envPrefix}_DB_SERVER={parser.Server}");
                    Console.WriteLine($"  {envPrefix}_DB_PORT={parser.Port}");
                    Console.WriteLine($"  {envPrefix}_DB_NAME={parser.DatabaseName}");
                    Console.WriteLine($"  {envPrefix}_DB_APP_USER={parser.AppUser}");
                    Console.WriteLine($"  {envPrefix}_DB_APP_PASSWORD=<set>");
                    if (parser.UseDocker)
                    {
                        Console.WriteLine($"  {envPrefix}_DB_SA_PASSWORD=<set>");
                        Console.WriteLine($"  {envPrefix}_DB_USE_DOCKER=true");
                    }
                    Console.WriteLine();
                    Console.WriteLine("Note: You may need to restart your terminal/IDE for the environment variables to take effect.");
                }

                logger.LogInformation("Database setup completed successfully!");
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database setup");
                Environment.Exit(1);
            }
        }
<<<<<<< HEAD
=======

        private static string BuildConnectionString(string server, int port, string database, string? userId = null, string? saPassword = null)
        {
            if (!string.IsNullOrEmpty(saPassword))
            {
                return $"Server={server},{port};Database={database};User Id={userId ?? "sa"};Password={saPassword};TrustServerCertificate=true;";
            }
            else
            {
                // Use Windows Authentication
                return $"Server={server};Database={database};Integrated Security=true;TrustServerCertificate=true;";
            }
        }
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
    }
}
