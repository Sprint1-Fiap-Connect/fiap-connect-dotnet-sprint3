using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FiapConnect.Application.DTOs.Conversa;
using FiapConnect.Infrastructure.Mongo;
using FiapConnect.IntegrationTests.Fixtures;
using FiapConnect.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace FiapConnect.IntegrationTests.Controllers;

[Collection("Api")]
public class ConversasControllerTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _fixture;
    private readonly List<string> _idsParaLimpar = new();

    public ConversasControllerTests(WebAppFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        var jwt = JwtTestHelper.GerarJwtParaTeste(fixture, "RM560384");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);
    }

    [Fact]
    public async Task CriarConversa_ComDadosValidos_Retorna201()
    {
        // Arrange
        var request = new CriarConversaRequest
        {
            Participantes = new List<string> { "RM560384", "RM111111" },
            ContextoGrupoId = 999
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ConversaResponse>();
        body.Should().NotBeNull();
        body!.IdConversa.Should().Be("RM111111_RM560384");
        if (!string.IsNullOrEmpty(body.Id))
        {
            _idsParaLimpar.Add(body.Id);
        }
    }

    [Fact]
    public async Task CriarConversa_ComRmsIguais_Retorna400()
    {
        // Arrange
        var request = new CriarConversaRequest
        {
            Participantes = new List<string> { "RM560384", "RM560384" },
            ContextoGrupoId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListarPorParticipante_SemJwt_Retorna401()
    {
        // Arrange
        var clientSemJwt = _fixture.CreateClient();

        // Act
        var response = await clientSemJwt.GetAsync("/api/conversas?rm=RM560384");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnviarMensagem_UsandoIdConversaLogicoNoPath_Retorna200()
    {
        // Arrange
        // Cria a conversa para ter um IdConversa valido. O service tem idempotencia,
        // entao mesmo que rode varias vezes seguidas continua valido
        var criarRequest = new CriarConversaRequest
        {
            Participantes = new List<string> { "RM560384", "RM111111" },
            ContextoGrupoId = 999
        };
        var criarResponse = await _client.PostAsJsonAsync("/api/conversas", criarRequest);
        criarResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        var conversaCriada = await criarResponse.Content.ReadFromJsonAsync<ConversaResponse>();
        conversaCriada.Should().NotBeNull();

        // Usa o IdConversa logico (RM111111_RM560384) no path, nao o ObjectId
        var idConversaLogico = conversaCriada!.IdConversa;
        var enviarRequest = new EnviarMensagemRequest
        {
            RemetenteRm = "RM560384",
            Texto = "Mensagem de teste de integracao"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/conversas/{idConversaLogico}/mensagens",
            enviarRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        if (!string.IsNullOrEmpty(conversaCriada.Id))
        {
            _idsParaLimpar.Add(conversaCriada.Id);
        }
    }

    [Fact]
    public async Task ObterPorId_UsandoIdConversaLogicoNoPath_Retorna200()
    {
        // Arrange
        var criarRequest = new CriarConversaRequest
        {
            Participantes = new List<string> { "RM560384", "RM111111" },
            ContextoGrupoId = 999
        };
        var criarResponse = await _client.PostAsJsonAsync("/api/conversas", criarRequest);
        var conversaCriada = await criarResponse.Content.ReadFromJsonAsync<ConversaResponse>();
        conversaCriada.Should().NotBeNull();

        var idConversaLogico = conversaCriada!.IdConversa;

        // Act
        var response = await _client.GetAsync($"/api/conversas/{idConversaLogico}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        if (!string.IsNullOrEmpty(conversaCriada.Id))
        {
            _idsParaLimpar.Add(conversaCriada.Id);
        }
    }

    [Fact]
    public async Task ObterPorId_UsandoObjectIdNoPath_ContinuaRetornando200()
    {
        // Arrange
        // Garantia de nao regressao: o caminho antigo com ObjectId hex
        // continua funcionando apos o fix da Rodada 7.F
        var criarRequest = new CriarConversaRequest
        {
            Participantes = new List<string> { "RM560384", "RM111111" },
            ContextoGrupoId = 999
        };
        var criarResponse = await _client.PostAsJsonAsync("/api/conversas", criarRequest);
        var conversaCriada = await criarResponse.Content.ReadFromJsonAsync<ConversaResponse>();
        conversaCriada.Should().NotBeNull();
        conversaCriada!.Id.Should().NotBeNullOrEmpty();

        // Usa o ObjectId hex no path
        var objectIdNoPath = conversaCriada.Id!;

        // Act
        var response = await _client.GetAsync($"/api/conversas/{objectIdNoPath}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _idsParaLimpar.Add(conversaCriada.Id!);
    }

    public void Dispose()
    {
        // Remove documentos criados nos testes para nao poluir o db de teste
        try
        {
            using var scope = _fixture.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
            var col = ctx.Database.GetCollection<BsonDocument>("mensagens");
            foreach (var id in _idsParaLimpar)
            {
                col.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id)));
            }
        }
        catch
        {
            // Falha de cleanup nao deve mascarar resultado do teste
        }
    }
}