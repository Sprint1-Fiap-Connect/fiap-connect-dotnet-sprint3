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

    [Fact]
    public async Task CriarNotificacao_ComDadosContextoMisturadoTipos_Retorna201()
    {
        // Arrange
        // Antes da Rodada 7.G, Dictionary<string, object> com qualquer valor nao-null
        // quebrava silenciosamente a serializacao BSON e retornava 500. O fix global do
        // ObjectSerializer permite agora tipos misturados (string, int, bool) no mesmo dict
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "RM560384",
            Tipo = "TESTE_DADOS_CONTEXTO",
            Titulo = "Teste integracao dados contexto",
            Mensagem = "Notificacao com dadosContexto populado misturando tipos",
            DadosContexto = new Dictionary<string, object>
            {
                { "campo_string", "valor de exemplo" },
                { "campo_int", 42 },
                { "campo_bool", true }
            },
            Prioridade = "NORMAL"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notificacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<NotificacaoResponse>();
        body.Should().NotBeNull();
        body!.DadosContexto.Should().NotBeNull();
        body.DadosContexto!.Should().ContainKey("campo_string");
        body.DadosContexto.Should().ContainKey("campo_int");
        body.DadosContexto.Should().ContainKey("campo_bool");
    }

    [Fact]
    public async Task CriarNotificacao_ComRmSemPrefixo_CanonizaERetorna201()
    {
        // Arrange
        // Prova a canonizacao tolerante: cliente manda "560384" e o service
        // canoniza pra "RM560384" antes de validar no Oracle e persistir
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "560384",
            Tipo = "TESTE_CANONIZACAO",
            Titulo = "Teste integracao canonizacao",
            Mensagem = "Notificacao com RM sem prefixo",
            Prioridade = "NORMAL"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notificacoes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<NotificacaoResponse>();
        body.Should().NotBeNull();
        body!.RmDestinatario.Should().Be("RM560384");
    }

    [Fact]
    public async Task ObterPorId_ComIdInvalido_Retorna404()
    {
        // Arrange
        // Antes da Rodada 7.G, qualquer string nao-hex no path quebrava o
        // serializer BSON e retornava 500. Agora o repository valida ObjectId
        // e retorna null, fazendo o service lancar RecursoNaoEncontradoException
        var idInvalido = "abc";

        // Act
        var response = await _client.GetAsync($"/api/notificacoes/{idInvalido}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}