using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Interfaces;
using MongoDB.Driver;

namespace FiapConnect.Infrastructure.Mongo.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly IMongoCollection<Auditoria> _colecao;

    public AuditoriaRepository(MongoContext context)
    {
        _colecao = context.Database.GetCollection<Auditoria>("auditoria");
    }

    public async Task<Auditoria?> ObterPorIdAsync(string id)
    {
        var filtro = Builders<Auditoria>.Filter.Eq(a => a.Id, id);
        return await _colecao.Find(filtro).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Auditoria>> ListarAsync()
    {
        return await _colecao.Find(Builders<Auditoria>.Filter.Empty).ToListAsync();
    }

    public async Task<IEnumerable<Auditoria>> ListarPorTabelaAsync(string tabela)
    {
        var filtro = Builders<Auditoria>.Filter.Eq(a => a.TabelaAfetada, tabela);
        var ordenacao = Builders<Auditoria>.Sort.Descending(a => a.DataOperacao);

        return await _colecao.Find(filtro).Sort(ordenacao).ToListAsync();
    }

    public async Task<Auditoria> RegistrarAsync(Auditoria auditoria)
    {
        await _colecao.InsertOneAsync(auditoria);
        return auditoria;
    }
}