namespace FiapConnect.Application.DTOs.Conversa;

// Preview enxuto para listagem de conversas. Nao traz o array de mensagens
// Para historico completo usar GET /api/conversas/{id} que retorna ConversaDetalhadaResponse
public class ConversaResponse
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
}