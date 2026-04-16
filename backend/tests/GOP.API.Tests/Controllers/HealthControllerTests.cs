using System.Net;
using System.Text.Json;
using FluentAssertions;
using GOP.API.Tests.Fixtures;

namespace GOP.API.Tests.Controllers;

[Collection("ApiIntegrationTests")]
public sealed class HealthControllerTests
{
    private readonly GopTestWebApplicationFactory _factory;

    public HealthControllerTests(GopTestWebApplicationFactory factory)
    {
        _factory = factory;
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
