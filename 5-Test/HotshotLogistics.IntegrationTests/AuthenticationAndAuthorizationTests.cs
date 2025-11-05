using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using HotshotLogistics.Api;

namespace HotshotLogistics.IntegrationTests
{
    [Collection("DatabaseCollection")]
    public class AuthenticationAndAuthorizationTests : IntegrationTestBase
    {
        public AuthenticationAndAuthorizationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetDrivers_WithoutAuthorizationHeader_Returns401()
        {
            using var client = Factory.CreateClient();
            var response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDrivers_WithDriverRole_Returns403()
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Driver");

            var response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetDrivers_WithAdminRole_Returns200()
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetOverdueInvoices_WithAdminRole_Returns200()
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var response = await client.GetAsync("/api/Billing/invoices/overdue");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetOverdueInvoices_WithDriverRole_Returns403()
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Driver");

            var response = await client.GetAsync("/api/Billing/invoices/overdue");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Request_WithInvalidAuthorizationScheme_Returns401()
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            var response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
