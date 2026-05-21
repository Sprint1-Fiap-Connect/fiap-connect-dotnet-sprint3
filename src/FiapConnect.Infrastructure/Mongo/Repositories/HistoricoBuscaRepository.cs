using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Interfaces;
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
        var filtro = Builders<HistoricoBusca>.Filter.Eq(h => h.Id, id);
        await _colecao.DeleteOneAsync(filtro);
    }
}