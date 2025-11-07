using System.CommandLine;

namespace HotshotLogistics.DbSetup;

public class ArgumentParser
{
<<<<<<< HEAD
    public string? Server { get; private set; }
    public string? SaConnectionString { get; private set; }
    public string? DatabaseName { get; private set; }
    public string? AppUser { get; private set; }
    public string? Password { get; set; }
=======
    public string? ProjectSlug { get; private set; }
    public string? Server { get; set; }
    public int Port { get; set; } = 1433;
    public string? DatabaseName { get; set; }
    public string? AppUser { get; set; }
    public string? AppPassword { get; set; }
    public string? SaPassword { get; set; }
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
    public bool NonInteractive { get; private set; }
    public bool Force { get; private set; }
    public bool UseDocker { get; private set; }
    public string? DockerComposeFile { get; private set; }

    public ArgumentParser(string[] args)
    {
        // Check for help before setting up commands
        if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
        {
            ShowHelp();
            Environment.Exit(0);
        }

<<<<<<< HEAD
        var serverOption = new Option<string>(
            name: "--server",
            description: "SQL Server instance (e.g., localhost\\SQLEXPRESS)")
=======
        var projectSlugOption = new Option<string>(
            name: "--project-slug",
            description: "Project identifier for environment variables (e.g., 'hotshot' creates HOTSHOT_DB_* vars)",
            getDefaultValue: () => "app")
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
        {
            IsRequired = false
        };

        var serverOption = new Option<string>(
            name: "--server",
            description: "SQL Server instance (e.g., localhost or localhost\\SQLEXPRESS)")
        {
            IsRequired = false
        };

        var portOption = new Option<int>(
            name: "--port",
            description: "SQL Server port",
            getDefaultValue: () => 1433)
        {
            IsRequired = false
        };

        var databaseNameOption = new Option<string>(
            name: "--db-name",
            description: "Target database name")
        {
            IsRequired = false
        };

        var appUserOption = new Option<string>(
            name: "--app-user",
            description: "Application database user/login name")
        {
            IsRequired = false
        };

        var appPasswordOption = new Option<string>(
            name: "--app-password",
            description: "Application user password (auto-generated if not provided)")
        {
            IsRequired = false
        };

        var saPasswordOption = new Option<string>(
            name: "--sa-password",
            description: "SA password for SQL Server")
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

        var useDockerOption = new Option<bool>(
            name: "--use-docker",
            description: "Use Docker container for SQL Server (starts container via docker-compose)")
        {
            IsRequired = false
        };

        var dockerComposeFileOption = new Option<string>(
            name: "--docker-compose-file",
            description: "Path to docker-compose.yml file")
        {
            IsRequired = false
        };

        var rootCommand = new RootCommand("Database Setup CLI - Automated database provisioning with Docker support");

        rootCommand.AddOption(projectSlugOption);
        rootCommand.AddOption(serverOption);
        rootCommand.AddOption(portOption);
        rootCommand.AddOption(databaseNameOption);
        rootCommand.AddOption(appUserOption);
        rootCommand.AddOption(appPasswordOption);
        rootCommand.AddOption(saPasswordOption);
        rootCommand.AddOption(nonInteractiveOption);
        rootCommand.AddOption(forceOption);
        rootCommand.AddOption(useDockerOption);
        rootCommand.AddOption(dockerComposeFileOption);

        rootCommand.SetHandler((context) =>
        {
            ProjectSlug = context.ParseResult.GetValueForOption(projectSlugOption);
            Server = context.ParseResult.GetValueForOption(serverOption);
            Port = context.ParseResult.GetValueForOption(portOption);
            DatabaseName = context.ParseResult.GetValueForOption(databaseNameOption);
            AppUser = context.ParseResult.GetValueForOption(appUserOption);
            AppPassword = context.ParseResult.GetValueForOption(appPasswordOption);
            SaPassword = context.ParseResult.GetValueForOption(saPasswordOption);
            NonInteractive = context.ParseResult.GetValueForOption(nonInteractiveOption);
            Force = context.ParseResult.GetValueForOption(forceOption);
            UseDocker = context.ParseResult.GetValueForOption(useDockerOption);
            DockerComposeFile = context.ParseResult.GetValueForOption(dockerComposeFileOption);
        });

        rootCommand.Invoke(args);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Description:");
<<<<<<< HEAD
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
=======
        Console.WriteLine("  Database Setup CLI - Automated database provisioning with Docker support");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --project-slug <slug>                          Project identifier for environment variables [default: app]");
        Console.WriteLine("  --server <server>                              SQL Server instance");
        Console.WriteLine("  --port <port>                                  SQL Server port [default: 1433]");
        Console.WriteLine("  --db-name <db-name>                            Target database name");
        Console.WriteLine("  --app-user <app-user>                          Application database user/login name");
        Console.WriteLine("  --app-password <app-password>                  Application user password (auto-generated if not provided)");
        Console.WriteLine("  --sa-password <sa-password>                    SA password for SQL Server");
        Console.WriteLine("  --use-docker                                   Use Docker container for SQL Server");
        Console.WriteLine("  --docker-compose-file <docker-compose-file>    Path to docker-compose.yml file");
        Console.WriteLine("  --non-interactive                              Run without interactive prompts");
        Console.WriteLine("  --force                                        Allow destructive operations");
        Console.WriteLine("  --version                                      Show version information");
        Console.WriteLine("  -?, -h, --help                                 Show help and usage information");
        Console.WriteLine();
        Console.WriteLine("Environment Variables (using project slug 'MYPROJECT'):");
        Console.WriteLine("  MYPROJECT_DB_SERVER                            SQL Server instance");
        Console.WriteLine("  MYPROJECT_DB_PORT                              SQL Server port");
        Console.WriteLine("  MYPROJECT_DB_NAME                              Database name");
        Console.WriteLine("  MYPROJECT_DB_APP_USER                          Application user name");
        Console.WriteLine("  MYPROJECT_DB_APP_PASSWORD                      Application user password");
        Console.WriteLine("  MYPROJECT_DB_SA_PASSWORD                       SA password");
        Console.WriteLine("  MYPROJECT_DB_USE_DOCKER                        Use Docker (true/false)");
        Console.WriteLine("  MYPROJECT_DB_DOCKER_COMPOSE_FILE               Path to docker-compose.yml");
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
    }

    public void ApplyEnvironmentOverrides()
    {
        // Build environment variable prefix from project slug
        var envPrefix = $"{ProjectSlug!.ToUpperInvariant()}_DB";

        // Apply environment variable fallbacks if CLI args not provided
        Server ??= Environment.GetEnvironmentVariable($"{envPrefix}_SERVER");
        
        var portStr = Environment.GetEnvironmentVariable($"{envPrefix}_PORT");
        if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out var envPort))
        {
            Port = envPort;
        }

        DatabaseName ??= Environment.GetEnvironmentVariable($"{envPrefix}_NAME");
        AppUser ??= Environment.GetEnvironmentVariable($"{envPrefix}_APP_USER");
        AppPassword ??= Environment.GetEnvironmentVariable($"{envPrefix}_APP_PASSWORD");
        SaPassword ??= Environment.GetEnvironmentVariable($"{envPrefix}_SA_PASSWORD");
        DockerComposeFile ??= Environment.GetEnvironmentVariable($"{envPrefix}_DOCKER_COMPOSE_FILE");

        var useDockerStr = Environment.GetEnvironmentVariable($"{envPrefix}_USE_DOCKER");
        if (!string.IsNullOrEmpty(useDockerStr) && bool.TryParse(useDockerStr, out var envUseDocker))
        {
            UseDocker = envUseDocker;
        }
    }

    public bool Validate(out string errorMessage)
    {
        errorMessage = string.Empty;

        // Project slug is always required
        if (string.IsNullOrEmpty(ProjectSlug))
        {
            errorMessage = "Project slug is required. This should not happen - check default value.";
            return false;
        }

        if (UseDocker)
        {
            // Set default docker-compose file path if not provided
            if (string.IsNullOrEmpty(DockerComposeFile))
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var solutionDirectory = FindSolutionDirectory(currentDirectory);
                if (solutionDirectory != null)
                {
                    DockerComposeFile = Path.Combine(solutionDirectory, "7-Deployment", "docker-compose.yml");
                }
            }

            if (string.IsNullOrEmpty(DockerComposeFile) || !File.Exists(DockerComposeFile))
            {
                errorMessage = $"docker-compose.yml file not found. Provide --docker-compose-file or ensure file exists at: {DockerComposeFile}";
                return false;
            }

            // Set default server for Docker
            Server ??= "localhost";
        }

        // Remaining validation will be done in interactive mode or Program.cs
        return true;
    }
<<<<<<< HEAD
=======

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
>>>>>>> f14b4059c2249f211bc56adac3e170621a2b47fb
}
