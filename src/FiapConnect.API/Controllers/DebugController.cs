using FiapConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

// Endpoint de diagnostico: confirma que o IOracleClient esta conseguindo
// chegar ao ORDS via proxy. Retorna status, headers e body brutos da resposta
// pra inspecao manual. Protegido por JWT pra nao expor diagnostico em producao
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IOracleClient _oracleClient;

    public DebugController(IOracleClient oracleClient)
    {
        _oracleClient = oracleClient;
    }

    // GET api/debug/ords
    // Bate em /fiapconnect/usuario/RM560384 e devolve a resposta crua do ORDS.
    // Util pra confirmar que o proxy esta funcional e que o ORDS esta acessivel
    [HttpGet("ords")]
    public async Task<IActionResult> TestarOrds()
    {
        HttpResponseMessage response;

        try
        {
            response = await _oracleClient.GetAsync("usuario/RM560384");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                erro = "Excecao ao chamar o ORDS",
                mensagem = ex.Message,
                tipo = ex.GetType().Name
            });
        }

        var body = await response.Content.ReadAsStringAsync();

        var headersResposta = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(
                h => h.Key,
                h => string.Join(", ", h.Value));

        return Ok(new
        {
            statusCode = (int)response.StatusCode,
            statusDescricao = response.ReasonPhrase,
            headersResposta,
            body = body.Length > 2000 ? body[..2000] + "... [truncado]" : body
        });
    }
}