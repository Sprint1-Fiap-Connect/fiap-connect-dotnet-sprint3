using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapConnect.API.Controllers;

/// <summary>
/// Endpoint temporario de debug para diagnosticar erros 403 do ORDS.
/// Permite testar a conectividade com o ORDS a partir do ambiente Railway
/// usando os mesmos headers que um browser Chrome enviaria.
/// </summary>
[ApiController]
[Route("debug")]
[AllowAnonymous]
public class DebugController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DebugController> _logger;

    private const string OrdsUrl =
        "https://oracleapex.com/ords/alexisrondo/fiapconnect/usuario/RM560384";

    public DebugController(IHttpClientFactory httpClientFactory, ILogger<DebugController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Faz um GET ao endpoint ORDS com headers de browser Chrome e retorna
    /// status, headers e body da resposta para diagnostico de erros 403.
    /// </summary>
    [HttpGet("ords-test")]
    public async Task<IActionResult> OrdsTest()
    {
        _logger.LogInformation("DEBUG: iniciando requisicao de teste ao ORDS em {Url}", OrdsUrl);

        var client = _httpClientFactory.CreateClient("ords-debug");

        using var request = new HttpRequestMessage(HttpMethod.Get, OrdsUrl);

        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Origin", "https://oracleapex.com");
        request.Headers.TryAddWithoutValidation("Referer", "https://oracleapex.com/");

        try
        {
            using var response = await client.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            var responseHeaders = response.Headers
                .Concat(response.Content.Headers)
                .ToDictionary(
                    h => h.Key,
                    h => string.Join(", ", h.Value));

            _logger.LogInformation(
                "DEBUG: ORDS respondeu com status {StatusCode}", (int)response.StatusCode);

            return Ok(new
            {
                url            = OrdsUrl,
                statusCode     = (int)response.StatusCode,
                statusText     = response.StatusCode.ToString(),
                requestHeaders = new
                {
                    userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                    origin    = "https://oracleapex.com",
                    referer   = "https://oracleapex.com/"
                },
                responseHeaders,
                body  = responseBody,
                error = (string?)null
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "DEBUG: falha de rede ao contactar o ORDS");

            return Ok(new
            {
                url            = OrdsUrl,
                statusCode     = (int?)null,
                statusText     = (string?)null,
                requestHeaders = new
                {
                    userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                    origin    = "https://oracleapex.com",
                    referer   = "https://oracleapex.com/"
                },
                responseHeaders = (object?)null,
                body  = (string?)null,
                error = ex.Message
            });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "DEBUG: timeout ao contactar o ORDS");

            return Ok(new
            {
                url            = OrdsUrl,
                statusCode     = (int?)null,
                statusText     = (string?)null,
                requestHeaders = new
                {
                    userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                    origin    = "https://oracleapex.com",
                    referer   = "https://oracleapex.com/"
                },
                responseHeaders = (object?)null,
                body  = (string?)null,
                error = $"Timeout: {ex.Message}"
            });
        }
    }
}
