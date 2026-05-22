using System.Net;
using FiapConnect.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FiapConnect.IntegrationTests.Controllers;

[Collection("Api")]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebAppFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Health_ComMongoEOrdsOk_Retorna200Healthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        // MapHealthChecks default retorna o status global como string pura
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_SemAutenticacao_RetornaOk()
    {
        // Arrange
        // Health check eh publico (sem [Authorize]). Confirma que nao exige Bearer
        var clientSemJwt = _client;

        // Act
        var response = await clientSemJwt.GetAsync("/health");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}