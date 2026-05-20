using FiapConnect.Application.DTOs.Notificacao;

namespace FiapConnect.Application.Services;

public interface INotificacaoService
{
    Task<NotificacaoResponse> CriarAsync(CriarNotificacaoRequest request);

    Task<NotificacaoResponse> ObterPorIdAsync(string id);

    Task<IEnumerable<NotificacaoResponse>> ListarPorDestinatarioAsync(string rmDestinatario, bool apenasNaoLidas);

    Task MarcarComoLidaAsync(string id);

    Task RemoverAsync(string id);
}