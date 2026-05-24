using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FiapConnect.Infrastructure.Mongo.Repositories;

public class HistoricoBuscaRepository : IHistoricoBuscaRepository
{
    private readonly IMongoCollection<HistoricoBusca> _colecao;

    public HistoricoBuscaRepository(MongoContext context)
    {
        _colecao = context.Database.GetCollection<HistoricoBusca>("historico_buscas");
    }

    public async Task<HistoricoBusca?> ObterPorIdAsync(string id)
    {
        // Valida formato antes de filtrar pelo _id. Sem isso, o serializer
        // BSON falha com input nao-hex e o erro vira 500 generico no middleware
        if (!ObjectId.TryParse(id, out _))
            return null;

        var filtro = Builders<HistoricoBusca>.Filter.Eq(h => h.Id, id);
        return await _colecao.Find(filtro).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<HistoricoBusca>> ListarPorAlunoAsync(string rmAluno)
    {
        var filtro = Builders<HistoricoBusca>.Filter.Eq(h => h.RmAluno, rmAluno);
        var ordenacao = Builders<HistoricoBusca>.Sort.Descending(h => h.Timestamp);
        return await _colecao.Find(filtro).Sort(ordenacao).ToListAsync();
    }

    public async Task<HistoricoBusca> RegistrarAsync(HistoricoBusca historico)
    {
        await _colecao.InsertOneAsync(historico);
        return historico;
    }

    public async Task RemoverAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return;

        var filtro = Builders<HistoricoBusca>.Filter.Eq(h => h.Id, id);
        await _colecao.DeleteOneAsync(filtro);
    }
}