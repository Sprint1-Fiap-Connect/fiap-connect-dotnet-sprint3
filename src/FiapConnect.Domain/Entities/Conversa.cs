namespace FiapConnect.Domain.Entities;

// Representa uma conversa entre dois alunos (documento da colecao mensagens).
// As mensagens individuais ficam embarcadas no array Mensagens.
public class Conversa
{
    // Id do documento no Mongo, o driver converte ObjectId para string.
    public string? Id { get; set; }

    // Identificadore, rm, nome 
    public string IdConversa { get; set; } = string.Empty;

    public List<string> Participantes { get; set; } = new();

    public List<string> NomesParticipantes { get; set; } = new();

    public DateTime DataInicio { get; set; }

    public DateTime DataUltimaMensagem { get; set; }

    public int TotalMensagens { get; set; }

    public int MensagensNaoLidas { get; set; }

    public int ContextoGrupoId { get; set; }
    
    public string StatusConversa { get; set; } = "ATIVA";

    public List<MensagemItem> Mensagens { get; set; } = new();
}