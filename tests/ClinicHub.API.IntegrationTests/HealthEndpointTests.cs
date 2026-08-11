using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClinicHub.API.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<ClinicHubApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ClinicHubApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Liveness_ReturnsOkAndEchoesCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "integration-test-correlation");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Contains("integration-test-correlation", values);
    }

    [Fact]
    public async Task Login_WhenRequestLimitIsExceeded_ReturnsTooManyRequests()
    {
        var request = new { email = "invalid", password = "short" };

        await _client.PostAsJsonAsync("/api/auth/login", request);
        await _client.PostAsJsonAsync("/api/auth/login", request);
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}

public sealed class ClinicHubApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SqlServer"] = "Server=localhost;Database=ClinicHubTests;User Id=sa;Password=ClinicHub_dev_2026!;TrustServerCertificate=True",
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
            ["RabbitMq:ConnectionString"] = "amqp://clinichub:clinichub-dev@localhost:5672",
            ["Jwt:Issuer"] = "ClinicHub",
            ["Jwt:Audience"] = "ClinicHub.Web",
            ["Jwt:Key"] = "integration-test-key-with-at-least-32-characters",
            ["RateLimiting:Login:PermitLimit"] = "2",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        }));
    }
}
