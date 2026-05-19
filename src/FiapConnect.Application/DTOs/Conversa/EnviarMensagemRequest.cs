namespace FiapConnect.Application.DTOs.Conversa;

// Payload para enviar uma mensagem dentro de uma conversa existente
public class EnviarMensagemRequest
{
    public string RmRemetente { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
}