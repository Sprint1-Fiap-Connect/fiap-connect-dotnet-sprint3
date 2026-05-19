namespace FiapConnect.Application.DTOs.Conversa;

// Representa uma mensagem individual no historico de uma conversa
// NomeRemetente eh resolvido pelo service (via ORDS ou cache)
public class MensagemResponse
{
    public string RemetenteRm { get; set; } = string.Empty;
    public string NomeRemetente { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Lida { get; set; }
}