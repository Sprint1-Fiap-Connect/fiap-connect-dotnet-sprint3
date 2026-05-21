using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Interfaces;
using MongoDB.Driver;

namespace FiapConnect.Infrastructure.Mongo.Repositories;

public class NotificacaoRepository : INotificacaoRepository
{
    private readonly IMongoCollection<Notificacao> _colecao;

    public NotificacaoRepository(MongoContext context)
    {
        _colecao = context.Database.GetCollection<Notificacao>("notificacoes");
    }

    public async Task<Notificacao?> ObterPorIdAsync(string id)
    {
        var filtro = Builders<Notificacao>.Filter.Eq(n => n.Id, id);
        return await _colecao.Find(filtro).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Notificacao>> ListarPorDestinatarioAsync(string rmDestinatario, bool apenasNaoLidas)
    {
        var filtroBase = Builders<Notificacao>.Filter.Eq(n => n.RmDestinatario, rmDestinatario);

        // Quando apenasNaoLidas=true, combina com filtro de lida=false
        var filtroFinal = apenasNaoLidas
            ? filtroBase & Builders<Notificacao>.Filter.Eq(n => n.Lida, false)
            : filtroBase;

        return await _colecao.Find(filtroFinal).ToListAsync();
    }

    public async Task<Notificacao> CriarAsync(Notificacao notificacao)
    {
        await _colecao.InsertOneAsync(notificacao);
        return notificacao;
    }

    public async Task MarcarComoLidaAsync(string id)
    {
        var filtro = Builders<Notificacao>.Filter.Eq(n => n.Id, id);
        var update = Builders<Notificacao>.Update
            .Set(n => n.Lida, true)
            .Set(n => n.DataLeitura, DateTime.UtcNow);

        await _colecao.UpdateOneAsync(filtro, update);
    }

    public async Task RemoverAsync(string id)
    {
        var filtro = Builders<Notificacao>.Filter.Eq(n => n.Id, id);
        await _colecao.DeleteOneAsync(filtro);
    }
}