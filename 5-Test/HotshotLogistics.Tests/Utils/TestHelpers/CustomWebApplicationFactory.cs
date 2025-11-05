using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotshotLogistics.Contracts.Services;

namespace HotshotLogistics.Tests.Utils.TestHelpers
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                // Register the test MockService as an IMappingService so MappingServiceFactory can find it
                services.AddSingleton<IMappingService, MockService>();

                // Optionally, you can override or add other test doubles here.

            });

            base.ConfigureWebHost(builder);
        }
    }
}
