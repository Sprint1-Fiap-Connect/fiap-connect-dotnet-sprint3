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
        // BaseAddress e headers Mozilla (anti-WAF Akamai) sao configurados na DI (3.F)
    }

    public async Task<Usuario?> ObterUsuarioPorRmAsync(string rm)
    {
        var response = await _httpClient.GetAsync($"usuario/{rm}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // ORDS pode devolver o objeto direto OU dentro de { items: [...] }
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
        // Health check do ORDS: tenta buscar RM560384 (sempre existe na tabela USUARIO)
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
}