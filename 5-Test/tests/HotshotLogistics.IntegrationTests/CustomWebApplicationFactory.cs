// <copyright file="CustomWebApplicationFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.IntegrationTests
{
    using HotshotLogistics.Api;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Custom web application factory for integration tests that configures test authentication.
    /// </summary>
    /// <typeparam name="TProgram">The program type.</typeparam>
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Authentication scheme is configured in Program.cs for Development; avoid re-registering here to prevent "Scheme already exists: Test".

            // Configure test database connection string
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                if (string.IsNullOrEmpty(dbConnectionString))
                {
                    // Use default test connection string for local development using LocalDB
                    dbConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=HotshotLogisticsTest;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;";
                }

                // Add the connection string to the configuration so BaseRepository can find it
                var memoryConfigSource = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = dbConnectionString
                };
                config.AddInMemoryCollection(memoryConfigSource);
            });

            // Enable detailed logging for debugging
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Trace); // Show ALL logs including trace

                // Add specific loggers for troubleshooting
                logging.AddFilter("HotshotLogistics", LogLevel.Debug);
                logging.AddFilter("Microsoft.AspNetCore", LogLevel.Information);
                logging.AddFilter("HotshotLogistics.IntegrationTests", LogLevel.Trace);
            });

            // Use Development environment for better error messages
            builder.UseEnvironment("Development");
        }
    }
}
