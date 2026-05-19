namespace FiapConnect.Application.DTOs.Conversa;

// Resposta do GET /api/conversas/{id}: tudo do ConversaResponse + o array de mensagens
public class ConversaDetalhadaResponse
{
    public string? Id { get; set; }
    public string IdConversa { get; set; } = string.Empty;
    public int ContextoGrupoId { get; set; }
    public List<string> Participantes { get; set; } = new();
    public List<string> NomesParticipantes { get; set; } = new();
    public DateTime DataInicio { get; set; }
    public DateTime DataUltimaMensagem { get; set; }
    public int TotalMensagens { get; set; }
    public int MensagensNaoLidas { get; set; }
    public string StatusConversa { get; set; } = string.Empty;
    public List<MensagemResponse> Mensagens { get; set; } = new();
}