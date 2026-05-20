using FiapConnect.Application.DTOs.Conversa;

namespace FiapConnect.Application.Services;

public interface IConversaService
{
    Task<ConversaResponse> CriarAsync(CriarConversaRequest request);
    Task<ConversaDetalhadaResponse> ObterPorIdAsync(string id);
    Task<IEnumerable<ConversaResponse>> ListarPorParticipanteAsync(string rm);
    Task<MensagemResponse> EnviarMensagemAsync(string idConversa, EnviarMensagemRequest request);
    Task MarcarMensagensComoLidasAsync(string idConversa, string rmLeitor);
    Task RemoverAsync(string id);
}