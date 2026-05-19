using FiapConnect.Domain.Entities;

namespace FiapConnect.Domain.Interfaces;

public interface IConversaRepository
{
    Task<Conversa?> ObterPorIdAsync(string id);

    // Busca pela conversa entre dois RMs (em qualquer ordem dos participantes)
    Task<Conversa?> ObterEntreParticipantesAsync(string rm1, string rm2);

    Task<IEnumerable<Conversa>> ListarPorParticipanteAsync(string rm);

    Task<Conversa> CriarAsync(Conversa conversa);

    // Adiciona uma mensagem ao array embarcado e atualiza contadores/timestamps
    Task AdicionarMensagemAsync(string idConversa, MensagemItem mensagem);

    // Marca como lidas todas as mensagens de uma conversa onde o destinatario eh o RM informado
    Task MarcarMensagensComoLidasAsync(string idConversa, string rmLeitor);

    Task RemoverAsync(string id);
}