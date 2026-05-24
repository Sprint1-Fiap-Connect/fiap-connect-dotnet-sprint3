using System.Text.Json;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Entities;
using FiapConnect.Infrastructure.Oracle.Dto;
using Microsoft.Extensions.Logging;

namespace FiapConnect.Infrastructure.Oracle;

public class OracleClient : IOracleClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OracleClient> _logger;

    public OracleClient(HttpClient httpClient, ILogger<OracleClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Usuario?> ObterUsuarioPorRmAsync(string rm)
    {
        var response = await _httpClient.GetAsync($"usuario/{rm}");
        var json = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            // Trunca body para nao poluir log quando ORDS devolve HTML grande
            var bodyParaLog = json.Length > 1000 ? json[..1000] : json;
            _logger.LogWarning(
                "ORDS retornou erro {StatusCode} {ReasonPhrase}. Body: {Body}",
                (int)response.StatusCode, response.ReasonPhrase, bodyParaLog);
            return null;
        }

        UsuarioOrdsResponse? dto;

        if (json.Contains("\"items\""))
        {
            var wrapper = JsonSerializer.Deserialize<OrdsItemsWrapper<UsuarioOrdsResponse>>(json);
            dto = wrapper?.Items?.FirstOrDefault();
        }
        else
        {
            dto = JsonSerializer.Deserialize<UsuarioOrdsResponse>(json);
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Rm))
        {
            return null;
        }

        return new Usuario
        {
            Rm = dto.Rm,
            NomeCompleto = dto.NomeCompleto ?? string.Empty,
            EmailInstitucional = dto.EmailInstitucional ?? string.Empty
        };
    }

    public async Task<bool> EstaSaudavelAsync()
    {
        try
        {
            var usuario = await ObterUsuarioPorRmAsync("RM560384");
            return usuario != null;
        }
        catch
        {
            return false;
        }
    }

    // Delega ao HttpClient tipado para o DebugController inspecionar a resposta
    // bruta (status, headers, body) sem transformacao. Os headers configurados
    // no DependencyInjection sao enviados automaticamente
    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
        => _httpClient.GetAsync(relativeUrl);
}