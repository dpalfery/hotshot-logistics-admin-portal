using System.CommandLine;

namespace HotshotLogistics.DbSetup;

public class ArgumentParser
{
    public string? Server { get; private set; }
    public string? SaConnectionString { get; private set; }
    public string? DatabaseName { get; private set; }
    public string? AppUser { get; private set; }
    public string? Password { get; set; }
    public bool NonInteractive { get; private set; }
    public bool Force { get; private set; }
    public bool PersistEnvironment { get; private set; }

    public ArgumentParser(string[] args)
    {
        // Check for help before setting up commands
        if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
        {
            ShowHelp();
            Environment.Exit(0);
        }

        var serverOption = new Option<string>(
            name: "--server",
            description: "SQL Server instance (e.g., localhost\\SQLEXPRESS)")
        {
            IsRequired = false
        };

        var saConnectionStringOption = new Option<string>(
            name: "--sa-connection-string",
            description: "SA or privileged connection string for non-interactive mode")
        {
            IsRequired = false
        };

        var databaseNameOption = new Option<string>(
            name: "--db-name",
            description: "Target database name",
            getDefaultValue: () => "hotshot_logistics")
        {
            IsRequired = false
        };

        var appUserOption = new Option<string>(
            name: "--app-user",
            description: "Application database user/login name",
            getDefaultValue: () => "hotshot_app")
        {
            IsRequired = false
        };

        var passwordOption = new Option<string>(
            name: "--password",
            description: "Application user password (not recommended for CI; prefer env var)")
        {
            IsRequired = false
        };

        var nonInteractiveOption = new Option<bool>(
            name: "--non-interactive",
            description: "Run without interactive prompts for CI")
        {
            IsRequired = false
        };

        var forceOption = new Option<bool>(
            name: "--force",
            description: "Allow destructive operations")
        {
            IsRequired = false
        };

        var persistEnvironmentOption = new Option<bool>(
            name: "--persist-env",
            description: "Persist password to system environment variable (requires explicit consent)")
        {
            IsRequired = false
        };

        var rootCommand = new RootCommand("Hotshot Logistics Database Setup CLI");

        rootCommand.AddOption(serverOption);
        rootCommand.AddOption(saConnectionStringOption);
        rootCommand.AddOption(databaseNameOption);
        rootCommand.AddOption(appUserOption);
        rootCommand.AddOption(passwordOption);
        rootCommand.AddOption(nonInteractiveOption);
        rootCommand.AddOption(forceOption);
        rootCommand.AddOption(persistEnvironmentOption);

        rootCommand.SetHandler((server, saConnectionString, dbName, appUser, password, nonInteractive, force, persistEnv) =>
        {
            Server = server;
            SaConnectionString = saConnectionString;
            DatabaseName = dbName;
            AppUser = appUser;
            Password = password;
            NonInteractive = nonInteractive;
            Force = force;
            PersistEnvironment = persistEnv;
        },
        serverOption, saConnectionStringOption, databaseNameOption, appUserOption,
        passwordOption, nonInteractiveOption, forceOption, persistEnvironmentOption);

        rootCommand.Invoke(args);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Description:");
        Console.WriteLine("  Hotshot Logistics Database Setup CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  HotshotLogistics.DbSetup [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --server <server>                              SQL Server instance (e.g., localhost\\SQLEXPRESS)");
        Console.WriteLine("  --sa-connection-string <sa-connection-string>  SA or privileged connection string for non-interactive mode");
        Console.WriteLine("  --db-name <db-name>                            Target database name [default: hotshot_logistics]");
        Console.WriteLine("  --app-user <app-user>                          Application database user/login name [default: hotshot_app]");
        Console.WriteLine("  --password <password>                          Application user password (not recommended for CI; prefer env var)");
        Console.WriteLine("  --non-interactive                              Run without interactive prompts for CI");
        Console.WriteLine("  --force                                        Allow destructive operations");
        Console.WriteLine("  --persist-env                                  Persist password to system environment variable (requires explicit consent)");
        Console.WriteLine("  --version                                      Show version information");
        Console.WriteLine("  -?, -h, --help                                 Show help and usage information");
    }

    public void ApplyEnvironmentOverrides()
    {
        // Apply environment variable fallbacks if CLI args not provided or using defaults
        Server ??= Environment.GetEnvironmentVariable("HOTSHOT_DB_SERVER");

        // Apply env var if DatabaseName is still the default value
        if (DatabaseName == "hotshot_logistics")
        {
            var envDbName = Environment.GetEnvironmentVariable("HOTSHOT_DB_NAME");
            if (!string.IsNullOrEmpty(envDbName))
            {
                DatabaseName = envDbName;
            }
        }

        // Apply env var if AppUser is still the default value
        if (AppUser == "hotshot_app")
        {
            var envAppUser = Environment.GetEnvironmentVariable("HOTSHOT_DB_APP_USER");
            if (!string.IsNullOrEmpty(envAppUser))
            {
                AppUser = envAppUser;
            }
        }

        Password ??= Environment.GetEnvironmentVariable("HOTSHOT_DB_PASSWORD");
        SaConnectionString ??= Environment.GetEnvironmentVariable("CI_SA_CONNECTION_STRING");
    }

    public bool Validate(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (NonInteractive && string.IsNullOrEmpty(SaConnectionString))
        {
            errorMessage = "SA connection string is required in non-interactive mode. Provide --sa-connection-string or set CI_SA_CONNECTION_STRING environment variable.";
            return false;
        }

        if (string.IsNullOrEmpty(Server))
        {
            errorMessage = "Server is required. Provide --server or set HOTSHOT_DB_SERVER environment variable.";
            return false;
        }

        return true;
    }
}
