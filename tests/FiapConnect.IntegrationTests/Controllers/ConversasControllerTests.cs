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