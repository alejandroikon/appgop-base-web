using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GOP.API.Tests.Fixtures;

namespace GOP.API.Tests.Controllers;

[Collection("ApiIntegrationTests")]
public sealed class CatalogsControllerTests
{
    private readonly GopTestWebApplicationFactory _factory;

    public CatalogsControllerTests(GopTestWebApplicationFactory factory)
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

    [Fact]
    public async Task ListContratos_WithValidToken_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/catalogs/contratos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        // En InMemory sin seed, retorna array vacío — verificamos el tipo de respuesta
        json.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ListCampos_WithContratoId_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/catalogs/campos?contratoId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ListMunicipios_WithDepartamentoId_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/catalogs/municipios?departamentoId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
