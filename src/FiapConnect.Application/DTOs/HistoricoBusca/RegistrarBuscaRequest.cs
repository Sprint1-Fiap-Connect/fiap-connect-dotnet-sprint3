namespace FiapConnect.Application.DTOs.HistoricoBusca;

// Payload para registrar uma busca feita pelo aluno. O NomeAluno e o Timestamp
// sao resolvidos pelo service (via ORDS e DateTime.UtcNow)
// TotalGruposRetornados eh calculado a partir do tamanho da lista GruposRetornados
public class RegistrarBuscaRequest
{
    public string RmAluno { get; set; } = string.Empty;
    public string FiltroDisciplina { get; set; } = string.Empty;
    public string EdicaoChallenge { get; set; } = string.Empty;
    public List<string> HabilidadesAluno { get; set; } = new();
    public List<GrupoRetornadoDto> GruposRetornados { get; set; } = new();
    public int? GrupoClicadoId { get; set; }
    public bool SolicitacaoEnviada { get; set; }
}