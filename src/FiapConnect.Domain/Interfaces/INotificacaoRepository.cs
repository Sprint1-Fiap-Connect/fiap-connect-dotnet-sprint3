using FiapConnect.Domain.Entities;

namespace FiapConnect.Domain.Interfaces;

public interface INotificacaoRepository
{
    Task<Notificacao?> ObterPorIdAsync(string id);

    // apenasNaoLidas=true filtra so as nao lidas
    Task<IEnumerable<Notificacao>> ListarPorDestinatarioAsync(string rmDestinatario, bool apenasNaoLidas);

    Task<Notificacao> CriarAsync(Notificacao notificacao);

    Task MarcarComoLidaAsync(string id);

    Task RemoverAsync(string id);
}