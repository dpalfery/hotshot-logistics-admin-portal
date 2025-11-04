using Microsoft.Extensions.Logging;
using HotshotLogistics.DbSetup;

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
    var commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
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
    var saConnectionString = parser.SaConnectionString ??
        $"Server={parser.Server};Database=master;Integrated Security=true;TrustServerCertificate=true;";

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

    // Build application connection string
    var appConnectionString = $"Server={parser.Server};Database={parser.DatabaseName};User Id={parser.AppUser};Password={appPassword};TrustServerCertificate=true;";

    // Persist connection string with user consent if not in non-interactive mode
    if (parser.PersistEnvironment)
    {
        environmentManager.PersistWithConsent("DB_CONNECTION_STRING", appConnectionString, parser.NonInteractive);
    }
    else if (!parser.NonInteractive)
    {
        Console.WriteLine();
        Console.WriteLine("Connection string generated:");
        Console.WriteLine(appConnectionString);
        Console.WriteLine();
        Console.WriteLine("You can set this as an environment variable:");
        Console.WriteLine($"  DB_CONNECTION_STRING={appConnectionString}");
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
