using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FiapConnect.Application.DTOs.Notificacao;
using FiapConnect.IntegrationTests.Fixtures;
using FiapConnect.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace FiapConnect.IntegrationTests.Controllers;

[Collection("Api")]
public class NotificacoesControllerTests
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _fixture;

    public NotificacoesControllerTests(WebAppFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        var jwt = JwtTestHelper.GerarJwtParaTeste(fixture, "RM560384");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);
    }

    [Fact]
    public async Task ListarPorDestinatario_SemJwt_Retorna401()
    {
        // Arrange
        var clientSemJwt = _fixture.CreateClient();

        // Act
        var response = await clientSemJwt.GetAsync(
            "/api/notificacoes?rmDestinatario=RM560384");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriarNotificacao_ComDestinatarioInexistente_Retorna404()
    {
        // Arrange
        // RM999998 nao existe na tabela USUARIO do Oracle (count=0 na resposta),
        // entao o service lanca RecursoNaoEncontradoException e o middleware retorna 404
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "RM999998",
            Tipo = "INFO",
            Titulo = "Teste integracao",
            Mensagem = "Notificacao de teste para destinatario inexistente",
            Prioridade = "NORMAL"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notificacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}