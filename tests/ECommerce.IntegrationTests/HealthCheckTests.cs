using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests;

public class HealthCheckTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests()
    {
        _factory = TestDatabase.CreateFactory("HealthCheck");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Liveness_Endpoint_Returns_Healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_Endpoint_Returns_Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_Document_Is_Available()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}