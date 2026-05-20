using FiapConnect.Application.DTOs.Notificacao;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;

namespace FiapConnect.Application.Services;

public class NotificacaoService : INotificacaoService
{
    private readonly INotificacaoRepository _repository;
    private readonly IOracleClient _oracleClient;

    public NotificacaoService(INotificacaoRepository repository, IOracleClient oracleClient)
    {
        _repository = repository;
        _oracleClient = oracleClient;
    }

    public async Task<NotificacaoResponse> CriarAsync(CriarNotificacaoRequest request)
    {
        // Valida que o destinatario existe no Oracle antes de gravar a notificacao
        var destinatario = await _oracleClient.ObterUsuarioPorRmAsync(request.RmDestinatario);

        if (destinatario == null)
        {
            throw new RecursoNaoEncontradoException(
                $"Usuario com RM {request.RmDestinatario} nao encontrado no Oracle");
        }

        var notificacao = new Notificacao
        {
            RmDestinatario = request.RmDestinatario,
            Tipo = request.Tipo,
            Titulo = request.Titulo,
            Mensagem = request.Mensagem,
            DataEnvio = DateTime.UtcNow,
            Lida = false,
            DataLeitura = null,
            DadosContexto = request.DadosContexto,
            Prioridade = string.IsNullOrWhiteSpace(request.Prioridade) ? "NORMAL" : request.Prioridade,
            Origem = "DOTNET"
        };

        var criada = await _repository.CriarAsync(notificacao);
        return ParaResponse(criada);
    }

    public async Task<NotificacaoResponse> ObterPorIdAsync(string id)
    {
        var notificacao = await _repository.ObterPorIdAsync(id);

        if (notificacao == null)
        {
            throw new RecursoNaoEncontradoException($"Notificacao {id} nao encontrada");
        }

        return ParaResponse(notificacao);
    }

    public async Task<IEnumerable<NotificacaoResponse>> ListarPorDestinatarioAsync(string rmDestinatario, bool apenasNaoLidas)
    {
        var notificacoes = await _repository.ListarPorDestinatarioAsync(rmDestinatario, apenasNaoLidas);
        return notificacoes.Select(ParaResponse);
    }

    public async Task MarcarComoLidaAsync(string id)
    {
        // Garante que a notificacao existe antes de delegar pro repository
        var existente = await _repository.ObterPorIdAsync(id);

        if (existente == null)
        {
            throw new RecursoNaoEncontradoException($"Notificacao {id} nao encontrada");
        }

        await _repository.MarcarComoLidaAsync(id);
    }

    public async Task RemoverAsync(string id)
    {
        var existente = await _repository.ObterPorIdAsync(id);

        if (existente == null)
        {
            throw new RecursoNaoEncontradoException($"Notificacao {id} nao encontrada");
        }

        await _repository.RemoverAsync(id);
    }

    private static NotificacaoResponse ParaResponse(Notificacao n) => new()
    {
        Id = n.Id,
        RmDestinatario = n.RmDestinatario,
        Tipo = n.Tipo,
        Titulo = n.Titulo,
        Mensagem = n.Mensagem,
        DataEnvio = n.DataEnvio,
        Lida = n.Lida,
        DataLeitura = n.DataLeitura,
        DadosContexto = n.DadosContexto,
        Prioridade = n.Prioridade,
        Origem = n.Origem
    };
}