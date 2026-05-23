using System.Text.Json;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Entities;
using FiapConnect.Infrastructure.Oracle.Dto;

namespace FiapConnect.Infrastructure.Oracle;

public class OracleClient : IOracleClient
{
    private readonly HttpClient _httpClient;

    public OracleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
            Console.WriteLine($"ORDS retornou erro {(int)response.StatusCode} - {response.ReasonPhrase}");
            Console.WriteLine(json.Length > 1000 ? json[..1000] : json);
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

    // Delega diretamente ao HttpClient tipado para que o DebugController possa
    // inspecionar a resposta bruta (status, headers, body) sem nenhuma transformacao.
    // Todos os 9 headers configurados no DependencyInjection sao enviados automaticamente
    // pelo HttpClient tipado antes de a requisicao sair do Railway
    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
        => _httpClient.GetAsync(relativeUrl);
}