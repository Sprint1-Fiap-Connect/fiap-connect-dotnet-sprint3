using FiapConnect.Application.DTOs.Conversa;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;

namespace FiapConnect.Application.Services;

public class ConversaService : IConversaService
{
    private readonly IConversaRepository _repository;
    private readonly IOracleClient _oracleClient;

    public ConversaService(IConversaRepository repository, IOracleClient oracleClient)
    {
        _repository = repository;
        _oracleClient = oracleClient;
    }

    public async Task<ConversaResponse> CriarAsync(CriarConversaRequest request)
    {
        if (request is null)
            throw new RegraDeNegocioException("Request nao informado");

        if (request.Participantes is null || request.Participantes.Count != 2)
            throw new RegraDeNegocioException("Uma conversa deve ter exatamente 2 participantes");

        var rm1 = request.Participantes[0];
        var rm2 = request.Participantes[1];

        if (rm1 == rm2)
            throw new RegraDeNegocioException("Os participantes devem ter RMs diferentes");

        // Idempotencia: se ja existe conversa entre os 2 RMs, retorna a existente
        var existente = await _repository.ObterEntreParticipantesAsync(rm1, rm2);
        if (existente != null)
            return MapearParaResponse(existente);

        // Valida que ambos os RMs existem no Oracle (sequencial pra simplicidade)
        var usuario1 = await _oracleClient.ObterUsuarioPorRmAsync(rm1);
        if (usuario1 == null)
            throw new RecursoNaoEncontradoException($"Usuario com RM {rm1} nao encontrado no Oracle");

        var usuario2 = await _oracleClient.ObterUsuarioPorRmAsync(rm2);
        if (usuario2 == null)
            throw new RecursoNaoEncontradoException($"Usuario com RM {rm2} nao encontrado no Oracle");

        // IdConversa deterministico ordenando os RMs
        var participantesOrdenados = request.Participantes.OrderBy(r => r).ToList();
        var idConversa = string.Join("_", participantesOrdenados);

        // NomesParticipantes na mesma ordem dos Participantes ordenados
        var nomesParticipantes = participantesOrdenados
            .Select(rm => rm == usuario1.Rm ? usuario1.NomeCompleto : usuario2.NomeCompleto)
            .ToList();

        var agora = DateTime.UtcNow;

        var conversa = new Conversa
        {
            IdConversa = idConversa,
            Participantes = participantesOrdenados,
            NomesParticipantes = nomesParticipantes,
            DataInicio = agora,
            DataUltimaMensagem = agora,
            TotalMensagens = 0,
            MensagensNaoLidas = 0,
            ContextoGrupoId = request.ContextoGrupoId,
            StatusConversa = "ATIVA",
            Mensagens = new List<MensagemItem>()
        };

        var criada = await _repository.CriarAsync(conversa);
        return MapearParaResponse(criada);
    }

    public async Task<ConversaDetalhadaResponse> ObterPorIdAsync(string id)
    {
        var conversa = await _repository.ObterPorIdAsync(id);

        if (conversa == null)
            throw new RecursoNaoEncontradoException($"Conversa {id} nao encontrada");

        return MapearParaDetalhada(conversa);
    }

    public async Task<IEnumerable<ConversaResponse>> ListarPorParticipanteAsync(string rm)
    {
        var conversas = await _repository.ListarPorParticipanteAsync(rm);
        return conversas.Select(MapearParaResponse);
    }

    public async Task<MensagemResponse> EnviarMensagemAsync(string idConversa, EnviarMensagemRequest request)
    {
        if (request is null)
            throw new RegraDeNegocioException("Request nao informado");

        if (string.IsNullOrWhiteSpace(request.Texto))
            throw new RegraDeNegocioException("Texto da mensagem nao pode ser vazio");

        var conversa = await _repository.ObterPorIdAsync(idConversa);
        if (conversa == null)
            throw new RecursoNaoEncontradoException($"Conversa {idConversa} nao encontrada");

        if (!conversa.Participantes.Contains(request.RemetenteRm))
            throw new RegraDeNegocioException(
                $"RM {request.RemetenteRm} nao eh participante da conversa");

        if (conversa.StatusConversa != "ATIVA")
            throw new RegraDeNegocioException(
                $"Conversa esta com status {conversa.StatusConversa} e nao aceita novas mensagens");

        var mensagem = new MensagemItem
        {
            RemetenteRm = request.RemetenteRm,
            Texto = request.Texto,
            Timestamp = DateTime.UtcNow,
            Lida = false
        };

        // Repository cuida de atualizar TotalMensagens, MensagensNaoLidas e DataUltimaMensagem
        await _repository.AdicionarMensagemAsync(idConversa, mensagem);

        return MapearMensagemParaResponse(mensagem, conversa);
    }

    public async Task MarcarMensagensComoLidasAsync(string idConversa, string rmLeitor)
    {
        var conversa = await _repository.ObterPorIdAsync(idConversa);
        if (conversa == null)
            throw new RecursoNaoEncontradoException($"Conversa {idConversa} nao encontrada");

        if (!conversa.Participantes.Contains(rmLeitor))
            throw new RegraDeNegocioException(
                $"RM {rmLeitor} nao eh participante da conversa");

        await _repository.MarcarMensagensComoLidasAsync(idConversa, rmLeitor);
    }

    public async Task RemoverAsync(string id)
    {
        var existente = await _repository.ObterPorIdAsync(id);

        if (existente == null)
            throw new RecursoNaoEncontradoException($"Conversa {id} nao encontrada");

        await _repository.RemoverAsync(id);
    }

    private static ConversaResponse MapearParaResponse(Conversa c) => new()
    {
        Id = c.Id,
        IdConversa = c.IdConversa,
        ContextoGrupoId = c.ContextoGrupoId,
        Participantes = c.Participantes,
        NomesParticipantes = c.NomesParticipantes,
        DataInicio = c.DataInicio,
        DataUltimaMensagem = c.DataUltimaMensagem,
        TotalMensagens = c.TotalMensagens,
        MensagensNaoLidas = c.MensagensNaoLidas,
        StatusConversa = c.StatusConversa
    };

    private static ConversaDetalhadaResponse MapearParaDetalhada(Conversa c) => new()
    {
        Id = c.Id,
        IdConversa = c.IdConversa,
        ContextoGrupoId = c.ContextoGrupoId,
        Participantes = c.Participantes,
        NomesParticipantes = c.NomesParticipantes,
        DataInicio = c.DataInicio,
        DataUltimaMensagem = c.DataUltimaMensagem,
        TotalMensagens = c.TotalMensagens,
        MensagensNaoLidas = c.MensagensNaoLidas,
        StatusConversa = c.StatusConversa,
        Mensagens = c.Mensagens.Select(m => MapearMensagemParaResponse(m, c)).ToList()
    };

    // NomeRemetente vem do snapshot NomesParticipantes da conversa pai
    private static MensagemResponse MapearMensagemParaResponse(MensagemItem m, Conversa c)
    {
        var indice = c.Participantes.IndexOf(m.RemetenteRm);
        var nomeRemetente = indice >= 0 && indice < c.NomesParticipantes.Count
            ? c.NomesParticipantes[indice]
            : string.Empty;

        return new MensagemResponse
        {
            RemetenteRm = m.RemetenteRm,
            NomeRemetente = nomeRemetente,
            Texto = m.Texto,
            Timestamp = m.Timestamp,
            Lida = m.Lida
        };
    }
}