namespace FiapConnect.Application.DTOs.Auditoria;

// Auditoria so tem Response: a gravacao acontece internamente nos services
// quando eventos do sistema disparam (criar conversa, enviar mensagem, etc)
// Nao existe CriarAuditoriaRequest porque o endpoint POST nao eh exposto
public class AuditoriaResponse
{
    public string? Id { get; set; }
    public string TabelaAfetada { get; set; } = string.Empty;
    public int IdRegistro { get; set; }
    public string TipoOperacao { get; set; } = string.Empty;
    public string RmUsuario { get; set; } = string.Empty;
    public string NomeUsuario { get; set; } = string.Empty;
    public DateTime DataOperacao { get; set; }
    public Dictionary<string, object>? DadosAntes { get; set; }
    public Dictionary<string, object>? DadosDepois { get; set; }
    public string IpOrigem { get; set; } = string.Empty;
    public string SistemaOrigem { get; set; } = string.Empty;
}