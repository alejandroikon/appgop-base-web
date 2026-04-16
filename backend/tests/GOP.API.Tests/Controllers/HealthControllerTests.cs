using FluentAssertions;
using GOP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Text.Json;

namespace GOP.API.Tests.Controllers;

public sealed class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Reemplazar SQL Server por InMemory para no depender de Docker en CI
                services.RemoveAll<DbContextOptions<GopDbContext>>();
                services.RemoveAll<GopDbContext>();

                services.AddDbContext<GopDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_Health"));
            });
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().BeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable],
            because: "el endpoint de health retorna 200 o 503 según el estado de las dependencias");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.TryGetProperty("status", out _).Should().BeTrue(
            "el body debe contener el campo 'status'");
    }
}
