using FiapConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

// Endpoint temporario de diagnostico: verifica se o IOracleClient configurado
// com os 9 headers Akamai (User-Agent Chrome 126, Accept, Accept-Language,
// Accept-Encoding, Origin, Referer, Sec-Fetch-Dest, Sec-Fetch-Mode, Sec-Fetch-Site)
// esta enviando-os corretamente a partir do IP do Railway.
// Usa o mesmo cliente tipado registrado no DependencyInjection, garantindo que
// nenhum header seja omitido em relacao ao que o WAF espera.
[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IOracleClient _oracleClient;

    public DebugController(IOracleClient oracleClient)
    {
        _oracleClient = oracleClient;
    }

    // GET api/debug/ords
    // Faz um GET em /fiapconnect/usuario/RM560384 usando o IOracleClient configurado
    // e devolve o status HTTP, os headers de resposta e o body bruto do ORDS.
    // Isso permite confirmar se os 9 headers estao sendo aceitos pelo WAF Akamai
    // quando a requisicao parte do IP do Railway.
    [HttpGet("ords")]
    [AllowAnonymous]
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

        // Coleta todos os headers de resposta (response headers + content headers)
        // para inspecao completa do que o ORDS / WAF devolveu
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
