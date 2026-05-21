using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FiapConnect.Infrastructure.Mongo;

// Contexto unico do Mongo. Registrado como Singleton no DI
// Le ConnectionString e DatabaseName do appsettings via secao "MongoDb"
public class MongoContext
{
    public IMongoDatabase Database { get; }

    public MongoContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var databaseName = configuration["MongoDb:DatabaseName"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configuracao MongoDb:ConnectionString nao informada");

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Configuracao MongoDb:DatabaseName nao informada");

        var client = new MongoClient(connectionString);
        Database = client.GetDatabase(databaseName);
    }
}