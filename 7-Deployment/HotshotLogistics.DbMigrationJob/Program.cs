using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.DbMigrationJob
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables() // This allows overriding settings with env vars (e.g. ConnectionStrings__DefaultConnection)
                .Build();

            // Create service provider
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                                           ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException("Connection string is not set. Please set ConnectionStrings:DefaultConnection or DB_CONNECTION_STRING environment variable.");
                    }

                    services
                        .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Information))
                        .AddFluentMigratorCore()
                        .ConfigureRunner(rb => rb
                            .AddSqlServer()
                            .WithGlobalConnectionString(connectionString)
                            .ScanIn(typeof(HotshotLogistics.Data.Migrations.CreateCustomersTable).Assembly).For.Migrations())
                        .AddTransient<MigrationJobRunner>();
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Hotshot Logistics Database Migration Job...");

            try
            {
                using (var scope = host.Services.CreateScope())
                {
                    var runner = scope.ServiceProvider.GetRequiredService<MigrationJobRunner>();
                    await runner.RunAsync();
                }
                
                logger.LogInformation("Database migration job completed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An error occurred during the database migration job.");
                return 1;
            }
        }
    }

    public class MigrationJobRunner
    {
        private readonly IMigrationRunner _runner;
        private readonly ILogger<MigrationJobRunner> _logger;

        public MigrationJobRunner(IMigrationRunner runner, ILogger<MigrationJobRunner> logger)
        {
            _runner = runner;
            _logger = logger;
        }

        public Task RunAsync()
        {
            _logger.LogInformation("Starting database migration...");
            
            // Run all migrations
            // This includes the seeding migrations if they are part of the assembly and pending
            if (_runner.HasMigrationsToApplyUp())
            {
                _logger.LogInformation("Found pending migrations. Applying...");
                _runner.MigrateUp();
                _logger.LogInformation("Migrations applied successfully.");
            }
            else
            {
                _logger.LogInformation("No pending migrations found. Database is up to date.");
            }

            return Task.CompletedTask;
        }
    }
}
