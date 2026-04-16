using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GOP.API.Tests.Fixtures;

namespace GOP.API.Tests.Controllers;

[Collection("ApiIntegrationTests")]
public sealed class AuthControllerTests
{
    private readonly GopTestWebApplicationFactory _factory;

    public AuthControllerTests(GopTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange
        var client = CreateClient();
        var request = new { email = "admin@gop.co", password = "Admin123*" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        json.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();
        accessToken.GetString().Should().NotBeNullOrEmpty();

        json.TryGetProperty("refreshToken", out var refreshToken).Should().BeTrue();
        refreshToken.GetString().Should().NotBeNullOrEmpty();

        json.TryGetProperty("expiresIn", out var expiresIn).Should().BeTrue();
        expiresIn.GetInt32().Should().Be(1800);

        json.TryGetProperty("user", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Arrange
        var client = CreateClient();
        var request = new { email = "admin@gop.co", password = "WrongPassword" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.TryGetProperty("detail", out var detail).Should().BeTrue();
        detail.GetString().Should().Be("Correo o contraseña incorrectos.");
    }

    [Fact]
    public async Task Login_EmptyBody_Returns422()
    {
        // Arrange
        var client = CreateClient();
        // Empty strings trigger FluentValidation (not null → model binding 400)
        var request = new { email = "", password = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.TryGetProperty("detail", out var detail).Should().BeTrue();
        detail.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithProfile()
    {
        // Arrange
        var client = CreateClient();

        // Login to get a token
        var loginRequest = new { email = "admin@gop.co", password = "Admin123*" };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginJson = JsonSerializer.Deserialize<JsonElement>(loginBody);
        var accessToken = loginJson.GetProperty("accessToken").GetString()!;

        // Act
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        json.TryGetProperty("email", out var email).Should().BeTrue();
        email.GetString().Should().Be("admin@gop.co");

        json.TryGetProperty("role", out var role).Should().BeTrue();
        role.GetString().Should().Be("ADMIN");
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        // Arrange
        var client = CreateClient();

        // Login to get a refresh token
        var loginRequest = new { email = "operador@gop.co", password = "Oper123*" };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginJson = JsonSerializer.Deserialize<JsonElement>(loginBody);
        var originalRefreshToken = loginJson.GetProperty("refreshToken").GetString()!;

        // Act
        var refreshRequest = new { refreshToken = originalRefreshToken };
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        json.TryGetProperty("accessToken", out var newAccessToken).Should().BeTrue();
        newAccessToken.GetString().Should().NotBeNullOrEmpty();

        // New refresh token must differ from original (single-use)
        json.TryGetProperty("refreshToken", out var newRefreshToken).Should().BeTrue();
        newRefreshToken.GetString().Should().NotBe(originalRefreshToken,
            because: "el refresh token es de un solo uso y debe ser reemplazado por uno nuevo");

        json.TryGetProperty("expiresIn", out _).Should().BeTrue();
    }
}
