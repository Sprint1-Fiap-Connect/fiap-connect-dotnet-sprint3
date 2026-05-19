namespace FiapConnect.Domain.Entities;

public class GrupoRetornado
{
	public int IdGrupo { get; set; }
	public string NomeGrupo { get; set; } = string.Empty;
	public int Percentual { get; set; }
	public string ClassificacaoIa { get; set; } = string.Empty; // ALTA, MEDIA, BAIXA
}