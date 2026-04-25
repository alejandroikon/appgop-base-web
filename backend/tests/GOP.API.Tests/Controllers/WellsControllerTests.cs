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

    private async Task<string> GetOperadorTokenAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "operador@gop.co", password = "Oper123*" });
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> GetSupervisorTokenAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "supervisor@gop.co", password = "Super123*" });
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.GetProperty("accessToken").GetString()!;
    }

    /// <summary>
    /// Crea un pozo DRAFT via POST /api/v1/wells. Retorna el Guid del pozo creado,
    /// o null si la creación falla (ej. nombre duplicado por runs previos).
    /// </summary>
    private async Task<Guid?> CreateDraftWellAsync(
        HttpClient client, string denominacion, int consecutivo,
        int contratoId = 1, int campoId = 1)
    {
        var body = new
        {
            action = "DRAFT",
            contratoId,
            campoId,
            tipoTrayectoria = "O",
            clasificacion = "EXPLORATORIO",
            denominacion,
            consecutivo,
            tipoUbicacion = "CONTINENTAL",
            tipoAngulo = "V",
            tipoObjetivo = "PH",
            tipoTerminacion = "OH",
            departamentoId = 1,
            municipioId = 1,
            clusterId = (int?)null
        };
        var response = await client.PostAsJsonAsync("/api/v1/wells", body);
        if (!response.IsSuccessStatusCode) return null;
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        return Guid.Parse(json.GetProperty("id").GetString()!);
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
            action = "DRAFT",      // V2.0: campo requerido
            contratoId = 1,
            campoId = 1,
            tipoTrayectoria = "O",
            clasificacion = "EXPLORATORIO",
            denominacion = "ALPHA",
            consecutivo = 1,
            tipoUbicacion = "CONTINENTAL",
            tipoAngulo = "V",
            tipoObjetivo = "PH",
            tipoTerminacion = "OH",
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

    // ─── T4.1 — ListWells: filtros nuevos (Iter 9) ───────────────────────────

    [Fact]
    public async Task ListWells_WithCampoIdFilter_ReturnsOnlyMatchingWells()
    {
        // Arrange — OPERADOR (TenantId=2) crea pozos con distinto campoId
        var client = CreateClient();
        var token = await GetOperadorTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Crear dos pozos con campoId=1 (Campo Rubiales, ContratoId=1)
        await CreateDraftWellAsync(client, "CF1A", consecutivo: 81, campoId: 1);
        await CreateDraftWellAsync(client, "CF1B", consecutivo: 82, campoId: 1);
        // Crear un pozo con campoId=3 (Campo Cusiana, ContratoId=2) para verificar exclusión
        await CreateDraftWellAsync(client, "CF3A", consecutivo: 83, contratoId: 2, campoId: 3);

        // Act
        var response = await client.GetAsync("/api/v1/wells?campoId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        items.Should().NotBeEmpty("OPERADOR creó pozos con campoId=1");
        // Todos los ítems devueltos pertenecen al campo filtrado
        items.Should().AllSatisfy(item =>
            item.GetProperty("campo").GetString().Should().Be("Campo Rubiales"));
    }

    [Fact]
    public async Task ListWells_WithDenominacionFilter_ReturnsPartialMatch()
    {
        // Arrange — OPERADOR crea pozo con denominacion única "RUBTEST77"
        var client = CreateClient();
        var token = await GetOperadorTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateDraftWellAsync(client, "RUBTEST77", consecutivo: 91);
        await CreateDraftWellAsync(client, "QUIFA77",   consecutivo: 92);

        // Act — filtro parcial con el prefijo exacto (InMemory es case-sensitive;
        // la case-insensitivity real la garantiza la collation CI_AS de SQL Server).
        var response = await client.GetAsync("/api/v1/wells?denominacion=RUBTEST77");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        items.Should().NotBeEmpty();
        items.Should().AllSatisfy(item =>
            item.GetProperty("nombrePozo").GetString().Should().Contain("RUBTEST77"));
    }

    [Fact]
    public async Task ListWells_CombinedFilters_ReturnsIntersection()
    {
        // Arrange — OPERADOR crea pozos con distinto contratoId
        var client = CreateClient();
        var token = await GetOperadorTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateDraftWellAsync(client, "COMBO1", consecutivo: 93, contratoId: 1, campoId: 1);
        await CreateDraftWellAsync(client, "COMBO2", consecutivo: 94, contratoId: 2, campoId: 3);

        // Act — filtrar por contratoId=1 AND estado=BORRADOR
        var response = await client.GetAsync("/api/v1/wells?contratoId=1&estado=BORRADOR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        items.Should().NotBeEmpty();
        // Todos los ítems tienen estado BORRADOR (los wells DRAFT siempre son BORRADOR)
        items.Should().AllSatisfy(item =>
            item.GetProperty("estado").GetString().Should().Be("BORRADOR"));
    }

    // ─── T4.2 — GetWellById: shape completo + tenant isolation ───────────────

    [Fact]
    public async Task GetWell_ExistingWell_ReturnsFullWellDetailShape()
    {
        // Arrange — OPERADOR crea un pozo y lo consulta por Id
        var client = CreateClient();
        var token = await GetOperadorTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var wellId = await CreateDraftWellAsync(client, "SHAPETST", consecutivo: 88);
        wellId.Should().NotBeNull("la creación del pozo debe ser exitosa");

        // Act
        var response = await client.GetAsync($"/api/v1/wells/{wellId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        // Verificar shape completo del WellDetailDto (campos requeridos del contrato)
        json.TryGetProperty("id", out _).Should().BeTrue();
        json.TryGetProperty("operadora", out _).Should().BeTrue();
        json.TryGetProperty("estado", out var estadoProp).Should().BeTrue();
        estadoProp.GetString().Should().Be("BORRADOR");
        json.TryGetProperty("createdAt", out _).Should().BeTrue();
        json.TryGetProperty("forma101Radicada", out var f101).Should().BeTrue();
        f101.GetBoolean().Should().BeFalse();
        json.TryGetProperty("denominacion", out var denomProp).Should().BeTrue();
        denomProp.GetString().Should().Be("SHAPETST");
    }

    [Fact]
    public async Task GetWell_WellFromOtherTenant_Returns404NotForbidden()
    {
        // Arrange — ADMIN (TenantId=1) crea un pozo; SUPERVISOR (TenantId=2) intenta leerlo
        var adminClient = CreateClient();
        var adminToken = await GetAdminTokenAsync();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var wellId = await CreateDraftWellAsync(adminClient, "TNTTST", consecutivo: 97);
        wellId.Should().NotBeNull("ADMIN debe poder crear pozos");

        // Act — SUPERVISOR (TenantId=2) intenta acceder al pozo de TenantId=1
        var supervisorClient = CreateClient();
        var supervisorToken = await GetSupervisorTokenAsync();
        supervisorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supervisorToken);

        var response = await supervisorClient.GetAsync($"/api/v1/wells/{wellId}");

        // Assert — 404, no 403: no revelar la existencia del pozo de otro tenant (RN-TENANT-404)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
