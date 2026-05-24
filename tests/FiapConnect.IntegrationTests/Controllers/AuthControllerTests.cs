using System.Net;
using System.Net.Http.Json;
using FiapConnect.Application.DTOs.Auth;
using FiapConnect.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FiapConnect.IntegrationTests.Controllers;

[Collection("Api")]
public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebAppFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task Login_ComIdTokenFirebaseValido_RetornaJwt()
    {
        // Arrange
        // IdToken Firebase real e curto-vivido (1h). Gerar via app Mobile no momento do teste
        // e exportar como $env:TEST_FIREBASE_IDTOKEN antes de rodar dotnet test
        var idTokenReal = Environment.GetEnvironmentVariable("TEST_FIREBASE_IDTOKEN");
        Skip.If(string.IsNullOrWhiteSpace(idTokenReal),
            "TEST_FIREBASE_IDTOKEN nao configurado");

        var request = new LoginRequest { IdToken = idTokenReal! };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.Rm.Should().StartWith("RM");
    }

    [Fact]
    public async Task Login_ComIdTokenInvalido_Retorna400()
    {
        // Arrange
        var request = new LoginRequest { IdToken = "token-invalido-qualquer" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_SemIdToken_Retorna400()
    {
        // Arrange
        var request = new LoginRequest { IdToken = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}