namespace FiapConnect.API.Hateoas;

// Envelope generico para respostas paginadas com links HATEOAS
public class RespostaPaginadaDto<T>
{
    public IEnumerable<T> Itens { get; set; } = new List<T>();
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalItens { get; set; }
    public int TotalPaginas { get; set; }
    public List<LinkDto> Links { get; set; } = new();
}