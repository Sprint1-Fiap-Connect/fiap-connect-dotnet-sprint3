using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Interfaces;
using MongoDB.Driver;

namespace FiapConnect.Infrastructure.Mongo.Repositories;

public class ConversaRepository : IConversaRepository
{
    private readonly IMongoCollection<Conversa> _colecao;

    public ConversaRepository(MongoContext context)
    {
        _colecao = context.Database.GetCollection<Conversa>("mensagens");
    }

    public async Task<Conversa?> ObterPorIdAsync(string id)
    {
        var filtro = Builders<Conversa>.Filter.Eq(c => c.Id, id);
        return await _colecao.Find(filtro).FirstOrDefaultAsync();
    }

    public async Task<Conversa?> ObterEntreParticipantesAsync(string rm1, string rm2)
    {
        // Independe da ordem: ambos os RMs precisam estar no array participantes
        var filtro = Builders<Conversa>.Filter.All(c => c.Participantes, new[] { rm1, rm2 });
        return await _colecao.Find(filtro).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Conversa>> ListarPorParticipanteAsync(string rm)
    {
        var filtro = Builders<Conversa>.Filter.AnyEq(c => c.Participantes, rm);
        return await _colecao.Find(filtro).ToListAsync();
    }

    public async Task<Conversa> CriarAsync(Conversa conversa)
    {
        await _colecao.InsertOneAsync(conversa);
        return conversa;
    }

    public async Task AdicionarMensagemAsync(string idConversa, MensagemItem mensagem)
    {
        var filtro = Builders<Conversa>.Filter.Eq(c => c.Id, idConversa);

        // Push da mensagem no array, incremento dos contadores e atualizacao do timestamp
        var update = Builders<Conversa>.Update
            .Push(c => c.Mensagens, mensagem)
            .Inc(c => c.TotalMensagens, 1)
            .Inc(c => c.MensagensNaoLidas, 1)
            .Set(c => c.DataUltimaMensagem, mensagem.Timestamp);

        await _colecao.UpdateOneAsync(filtro, update);
    }

    public async Task MarcarMensagensComoLidasAsync(string idConversa, string rmLeitor)
    {
        var filtro = Builders<Conversa>.Filter.Eq(c => c.Id, idConversa);

        // Marca como lidas apenas as mensagens cujo remetente NAO eh o leitor e que ainda nao foram lidas
        var update = Builders<Conversa>.Update
            .Set("mensagens.$[elem].lida", true)
            .Set(c => c.MensagensNaoLidas, 0);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                new MongoDB.Bson.BsonDocument
                {
                    { "elem.remetente_rm", new MongoDB.Bson.BsonDocument("$ne", rmLeitor) },
                    { "elem.lida", false }
                })
        };

        var opcoes = new UpdateOptions { ArrayFilters = arrayFilters };

        await _colecao.UpdateOneAsync(filtro, update, opcoes);
    }

    public async Task RemoverAsync(string id)
    {
        var filtro = Builders<Conversa>.Filter.Eq(c => c.Id, id);
        await _colecao.DeleteOneAsync(filtro);
    }
}