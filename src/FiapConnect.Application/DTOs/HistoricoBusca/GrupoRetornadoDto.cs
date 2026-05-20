namespace FiapConnect.Application.DTOs.HistoricoBusca;

// Item embarcado dentro do historico de busca: cada grupo que apareceu no resultado
public class GrupoRetornadoDto
{
    public int IdGrupo { get; set; }
    public string NomeGrupo { get; set; } = string.Empty;
    public int Percentual { get; set; }
    public string ClassificacaoIa { get; set; } = string.Empty;
}