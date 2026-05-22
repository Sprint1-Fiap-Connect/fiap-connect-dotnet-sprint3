namespace FiapConnect.API.Hateoas;

// Link HATEOAS: aponta para uma acao relacionada ao recurso atual (self, previous, next, etc)
public class LinkDto
{
    public string Href { get; set; } = string.Empty;
    public string Rel { get; set; } = string.Empty;
    public string Metodo { get; set; } = "GET";
}