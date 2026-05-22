using FiapConnect.Infrastructure.Mongo;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FiapConnect.API.HealthChecks;

// Verifica conectividade com o Mongo Atlas via comando ping nativo
public class MongoHealthCheck : IHealthCheck
{
    private readonly MongoContext _context;

    public MongoHealthCheck(MongoContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.RunCommandAsync(
                (Command<BsonDocument>)"{ping:1}",
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Mongo OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Mongo nao respondeu", ex);
        }
    }
}