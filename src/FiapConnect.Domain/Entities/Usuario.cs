namespace FiapConnect.Domain.Entities;

// O Oracle esta com todos os dados aqui so tem o necessario pra exibir ou validar
// dentro das features do .Net
public class Usuario
{
    public string Rm { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
}