using System.Text.Json;
using FiapConnect.Application.DTOs.Notificacao;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Helpers;
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
        var rmDestinatario = RmHelper.Canonizar(request.RmDestinatario);
        if (rmDestinatario is null)
            throw new RegraDeNegocioException("RmDestinatario em formato invalido");

        var destinatario = await _oracleClient.ObterUsuarioPorRmAsync(rmDestinatario);

        if (destinatario == null)
        {
            throw new RecursoNaoEncontradoException(
                $"Usuario com RM {rmDestinatario} nao encontrado no Oracle");
        }

        var notificacao = new Notificacao
        {
            RmDestinatario = rmDestinatario,
            Tipo = request.Tipo,
            Titulo = request.Titulo,
            Mensagem = request.Mensagem,
            DataEnvio = DateTime.UtcNow,
            Lida = false,
            DataLeitura = null,
            DadosContexto = NormalizarDadosContexto(request.DadosContexto),
            Prioridade = string.IsNullOrWhiteSpace(request.Prioridade)
                ? "NORMAL"
                : request.Prioridade,
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

    public async Task<IEnumerable<NotificacaoResponse>> ListarPorDestinatarioAsync(
        string rmDestinatario,
        bool apenasNaoLidas)
    {
        var notificacoes = await _repository.ListarPorDestinatarioAsync(rmDestinatario, apenasNaoLidas);
        return notificacoes.Select(ParaResponse);
    }

    public async Task MarcarComoLidaAsync(string id)
    {
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

    // Quando o ASP.NET deserializa Dictionary<string, object>, os valores chegam
    // como JsonElement (tipo do System.Text.Json). Esse tipo nao eh suportado
    // pelo driver Mongo. Esta funcao converte cada valor para o tipo primitivo
    // equivalente (int, double, string, bool) ou estrutura aninhada (Dictionary, List)
    // antes de persistir, preservando a forma original do JSON
    private const int ProfundidadeMaxima = 5;

    private static Dictionary<string, object>? NormalizarDadosContexto(Dictionary<string, object>? entrada)
    {
        if (entrada is null) return null;

        var saida = new Dictionary<string, object>(entrada.Count);
        foreach (var par in entrada)
        {
            saida[par.Key] = ExtrairValor(par.Value, profundidade: 0);
        }

        return saida;
    }

    private static object ExtrairValor(object valor, int profundidade)
    {
        // Protege contra JSON malicioso ou aninhamento excessivo
        if (profundidade >= ProfundidadeMaxima) return string.Empty;

        if (valor is not JsonElement el) return valor;

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,

            JsonValueKind.Object => el.EnumerateObject()
                .ToDictionary(
                    prop => prop.Name,
                    prop => ExtrairValor(prop.Value, profundidade + 1)),

            JsonValueKind.Array => el.EnumerateArray()
                .Select(item => ExtrairValor(item, profundidade + 1))
                .ToList(),

            _ => string.Empty
        };
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