namespace FiapConnect.Application.DTOs.Auth;

// Payload de login enviado pelo cliente.
public class LoginRequest
{
    public string Rm { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}