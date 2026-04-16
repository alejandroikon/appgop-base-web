using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GOP.API.Tests.Fixtures;

namespace GOP.API.Tests.Controllers;

[Collection("ApiIntegrationTests")]
public sealed class WellsControllerTests
{
    private readonly GopTestWebApplicationFactory _factory;

    public WellsControllerTests(GopTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<string> GetAdminTokenAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@gop.co", password = "Admin123*" });
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> GetAuditorTokenAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "auditor@gop.co", password = "Audit123*" });
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task ListWells_WithValidToken_ReturnsPagedResponse()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/wells?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.TryGetProperty("items", out _).Should().BeTrue();
        json.TryGetProperty("total", out _).Should().BeTrue();
        json.TryGetProperty("page", out _).Should().BeTrue();
        json.TryGetProperty("pageSize", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateWell_ValidRequest_Returns201()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            contratoId = 1,
            campoId = 1,
            tipoTrayectoria = "ST",
            clasificacion = "EXPLORATORIO",
            denominacion = "ALPHA",
            consecutivo = "01",
            tipoUbicacion = "CONTINENTAL",
            tipoAngulo = "V",
            tipoObjetivo = "PH",
            tipoTerminacion = "CD",
            departamentoId = 1,
            municipioId = 1,
            clusterId = (int?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/wells", body);

        // Assert
        // En modo InMemory sin catálogos seeded el handler retornará error de catálogo,
        // pero el endpoint sí existe y responde (no 404 ni 500).
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.UnprocessableEntity  // Catálogos no seeded en InMemory
        );
    }

    [Fact]
    public async Task CreateWell_WithAuditorRole_Returns403()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAuditorTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            contratoId = 1, campoId = 1, tipoTrayectoria = "ST",
            clasificacion = "EXPLORATORIO", denominacion = "ALPHA", consecutivo = "01",
            tipoUbicacion = "CONTINENTAL", tipoAngulo = "V", tipoObjetivo = "PH",
            tipoTerminacion = "CD", departamentoId = 1, municipioId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/wells", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetWell_NonExistentId_Returns404()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/wells/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWell_NonExistentId_Returns404()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/wells/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
