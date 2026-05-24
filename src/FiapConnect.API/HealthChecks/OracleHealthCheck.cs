using System.Net;
using FiapConnect.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FiapConnect.API.HealthChecks;

// Verifica disponibilidade do ORDS Apex consultando RM real e estavel (RM560384).
// Usa GetAsync bruto (em vez de ObterUsuarioPorRmAsync) para classificar
// corretamente respostas do WAF Akamai: 403 nao deve ser mascarado como
// "usuario nao existe", e sim sinalizado como Unhealthy para orquestradores
// (Kubernetes, App Service, etc) terem visibilidade real do problema.
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
        HttpResponseMessage response;

        try
        {
            response = await _oracleClient.GetAsync("usuario/RM560384");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ORDS nao respondeu", ex);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.OK =>
                HealthCheckResult.Healthy("ORDS OK"),

            HttpStatusCode.NotFound =>
                HealthCheckResult.Degraded(
                    "ORDS respondeu mas usuario teste RM560384 nao existe"),

            HttpStatusCode.Forbidden =>
                HealthCheckResult.Unhealthy(
                    "ORDS retornou 403 - possivel bloqueio do WAF Akamai"),

            _ => HealthCheckResult.Unhealthy(
                $"ORDS retornou status inesperado: {(int)response.StatusCode} {response.ReasonPhrase}")
        };
    }
}
