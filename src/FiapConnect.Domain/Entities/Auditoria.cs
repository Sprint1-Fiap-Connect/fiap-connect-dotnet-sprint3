namespace FiapConnect.Domain.Entities;

public class Auditoria
{
    public string? Id { get; set; }
    public string TabelaAfetada { get; set; } = string.Empty;
    public int IdRegistro { get; set; }
    public string TipoOperacao { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public string RmUsuario { get; set; } = string.Empty;
    public string NomeUsuario { get; set; } = string.Empty;
    public DateTime DataOperacao { get; set; }
    public Dictionary<string, object>? DadosAntes { get; set; }
    public Dictionary<string, object>? DadosDepois { get; set; }
    public string IpOrigem { get; set; } = string.Empty;
    public string SistemaOrigem { get; set; } = string.Empty; // APEX, DOTNET, MOBILE_APP
}