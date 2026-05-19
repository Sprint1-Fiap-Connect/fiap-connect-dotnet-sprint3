namespace FiapConnect.Application.DTOs.Auth;

// Resposta do login: token JWT e dados basicos do usuario autenticado.
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Rm { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
}