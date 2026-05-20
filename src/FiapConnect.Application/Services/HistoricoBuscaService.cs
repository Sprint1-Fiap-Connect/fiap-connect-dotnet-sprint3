using FiapConnect.Application.DTOs.HistoricoBusca;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;

namespace FiapConnect.Application.Services;

public class HistoricoBuscaService : IHistoricoBuscaService
{
    private readonly IHistoricoBuscaRepository _repository;
    private readonly IOracleClient _oracleClient;

    public HistoricoBuscaService(IHistoricoBuscaRepository repository, IOracleClient oracleClient)
    {
        _repository = repository;
        _oracleClient = oracleClient;
    }

    public async Task<HistoricoBuscaResponse> RegistrarAsync(RegistrarBuscaRequest request)
    {
        // Valida que o aluno existe no Oracle e captura o NomeAluno como snapshot
        var aluno = await _oracleClient.ObterUsuarioPorRmAsync(request.RmAluno);

        if (aluno == null)
        {
            throw new RecursoNaoEncontradoException(
                $"Aluno com RM {request.RmAluno} nao encontrado no Oracle");
        }

        var historico = new HistoricoBusca
        {
            RmAluno = request.RmAluno,
            NomeAluno = aluno.NomeCompleto,
            Timestamp = DateTime.UtcNow,
            FiltroDisciplina = request.FiltroDisciplina,
            EdicaoChallenge = request.EdicaoChallenge,
            HabilidadesAluno = request.HabilidadesAluno,
            TotalGruposRetornados = request.GruposRetornados.Count,
            GruposRetornados = request.GruposRetornados.Select(ParaEntity).ToList(),
            GrupoClicadoId = request.GrupoClicadoId,
            SolicitacaoEnviada = request.SolicitacaoEnviada
        };

        var registrado = await _repository.RegistrarAsync(historico);
        return ParaResponse(registrado);
    }

    public async Task<HistoricoBuscaResponse> ObterPorIdAsync(string id)
    {
        var historico = await _repository.ObterPorIdAsync(id);

        if (historico == null)
        {
            throw new RecursoNaoEncontradoException($"Historico de busca {id} nao encontrado");
        }

        return ParaResponse(historico);
    }

    public async Task<IEnumerable<HistoricoBuscaResponse>> ListarPorAlunoAsync(string rmAluno)
    {
        var historicos = await _repository.ListarPorAlunoAsync(rmAluno);
        return historicos.Select(ParaResponse);
    }

    public async Task RemoverAsync(string id)
    {
        var existente = await _repository.ObterPorIdAsync(id);

        if (existente == null)
        {
            throw new RecursoNaoEncontradoException($"Historico de busca {id} nao encontrado");
        }

        await _repository.RemoverAsync(id);
    }

    // Mapeamentos privados entre DTO e Entity dos grupos retornados
    private static GrupoRetornado ParaEntity(GrupoRetornadoDto dto) => new()
    {
        IdGrupo = dto.IdGrupo,
        NomeGrupo = dto.NomeGrupo,
        Percentual = dto.Percentual,
        ClassificacaoIa = dto.ClassificacaoIa
    };

    private static GrupoRetornadoDto ParaDto(GrupoRetornado g) => new()
    {
        IdGrupo = g.IdGrupo,
        NomeGrupo = g.NomeGrupo,
        Percentual = g.Percentual,
        ClassificacaoIa = g.ClassificacaoIa
    };

    private static HistoricoBuscaResponse ParaResponse(HistoricoBusca h) => new()
    {
        Id = h.Id,
        RmAluno = h.RmAluno,
        NomeAluno = h.NomeAluno,
        Timestamp = h.Timestamp,
        FiltroDisciplina = h.FiltroDisciplina,
        EdicaoChallenge = h.EdicaoChallenge,
        HabilidadesAluno = h.HabilidadesAluno,
        TotalGruposRetornados = h.TotalGruposRetornados,
        GruposRetornados = h.GruposRetornados.Select(ParaDto).ToList(),
        GrupoClicadoId = h.GrupoClicadoId,
        SolicitacaoEnviada = h.SolicitacaoEnviada
    };
}