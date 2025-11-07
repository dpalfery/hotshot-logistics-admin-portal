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

                logger.LogInformation("Starting Hotshot Logistics Database Setup");
                logger.LogInformation("Server: {Server}", parser.Server);
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database setup");
                Environment.Exit(1);
            }
        }
    }
}
