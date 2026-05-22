using FiapConnect.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FiapConnect.API.HealthChecks;

// Verifica disponibilidade do ORDS Apex consultando RM real e estavel (RM560384)
public class OracleHealthCheck : IHealthCheck
{
    private readonly IOracleClient _oracleClient;

    public OracleHealthCheck(IOracleClient oracleClient)
    {
        _oracleClient = oracleClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var usuario = await _oracleClient.ObterUsuarioPorRmAsync("RM560384");

            return usuario != null
                ? HealthCheckResult.Healthy("ORDS OK")
                : HealthCheckResult.Degraded("ORDS respondeu mas usuario teste nao existe");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ORDS nao respondeu", ex);
        }
    }
}