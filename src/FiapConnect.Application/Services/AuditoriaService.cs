using FiapConnect.Application.DTOs.Auditoria;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;

namespace FiapConnect.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepository _repository;

    public AuditoriaService(IAuditoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuditoriaResponse> ObterPorIdAsync(string id)
    {
        var auditoria = await _repository.ObterPorIdAsync(id);

        if (auditoria == null)
        {
            throw new RecursoNaoEncontradoException($"Auditoria {id} nao encontrada");
        }

        return ParaResponse(auditoria);
    }

    public async Task<IEnumerable<AuditoriaResponse>> ListarAsync()
    {
        var auditorias = await _repository.ListarAsync();
        return auditorias.Select(ParaResponse);
    }

    public async Task<IEnumerable<AuditoriaResponse>> ListarPorTabelaAsync(string tabela)
    {
        var auditorias = await _repository.ListarPorTabelaAsync(tabela);
        return auditorias.Select(ParaResponse);
    }

    public async Task RegistrarInternoAsync(
        string tabela,
        int idRegistro,
        string tipoOperacao,
        string rmUsuario,
        Dictionary<string, object>? dadosAntes,
        Dictionary<string, object>? dadosDepois)
    {
        var auditoria = new Auditoria
        {
            TabelaAfetada = tabela,
            IdRegistro = idRegistro,
            TipoOperacao = tipoOperacao,
            RmUsuario = rmUsuario,
            NomeUsuario = string.Empty,
            DataOperacao = DateTime.UtcNow,
            DadosAntes = dadosAntes,
            DadosDepois = dadosDepois,
            IpOrigem = string.Empty,
            SistemaOrigem = "DOTNET"
        };

        await _repository.RegistrarAsync(auditoria);
    }

    // Mapeamento Entity -> DTO. Mantido privado pois eh detalhe interno do service.
    private static AuditoriaResponse ParaResponse(Auditoria a) => new()
    {
        Id = a.Id,
        TabelaAfetada = a.TabelaAfetada,
        IdRegistro = a.IdRegistro,
        TipoOperacao = a.TipoOperacao,
        RmUsuario = a.RmUsuario,
        NomeUsuario = a.NomeUsuario,
        DataOperacao = a.DataOperacao,
        DadosAntes = a.DadosAntes,
        DadosDepois = a.DadosDepois,
        IpOrigem = a.IpOrigem,
        SistemaOrigem = a.SistemaOrigem
    };
}