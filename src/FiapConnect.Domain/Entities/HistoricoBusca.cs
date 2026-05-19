namespace FiapConnect.Domain.Entities;

// HistoricoBusca tem array embarcado de GrupoRetornado igual Conversa/MensagemItem
public class HistoricoBusca
{
    public string? Id { get; set; }
    public string RmAluno { get; set; } = string.Empty;
    public string NomeAluno { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FiltroDisciplina { get; set; } = string.Empty; // sigla ou SEM_PREFERENCIA
    public string EdicaoChallenge { get; set; } = string.Empty;
    public List<string> HabilidadesAluno { get; set; } = new();
    public int TotalGruposRetornados { get; set; }
    public List<GrupoRetornado> GruposRetornados { get; set; } = new();
    public int? GrupoClicadoId { get; set; }
    public bool SolicitacaoEnviada { get; set; }
}