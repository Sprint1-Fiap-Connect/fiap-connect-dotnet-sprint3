namespace FiapConnect.Application.DTOs.Conversa;

// Payload para criar uma nova conversa entre dois alunos
// O IdConversa eh gerado pelo backend ordenando os RMs alfanumericamente
public class CriarConversaRequest
{
    public int ContextoGrupoId { get; set; }
    public List<string> Participantes { get; set; } = new();
}