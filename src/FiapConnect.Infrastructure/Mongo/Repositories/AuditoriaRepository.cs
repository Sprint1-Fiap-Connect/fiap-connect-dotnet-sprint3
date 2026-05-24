using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Interfaces;
using MongoDB.Bson;
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
        // Valida formato antes de filtrar pelo _id. Sem isso, o serializer
        // BSON falha com input nao-hex e o erro vira 500 generico no middleware
        if (!ObjectId.TryParse(id, out _))
            return null;

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