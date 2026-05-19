namespace FiapConnect.Domain.Entities;

// Representa uma mensagem individual dentro de uma conversa
// Nao tem Id proprio porque vive embarcada no array da Conversa
public class MensagemItem
{
    public string RemetenteRm { get; set; } = string.Empty;

    public string Texto { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public bool Lida { get; set; }
}