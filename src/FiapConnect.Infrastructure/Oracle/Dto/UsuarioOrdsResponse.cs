using System.Text.Json.Serialization;

namespace FiapConnect.Infrastructure.Oracle.Dto;

// DTO interno pra deserializar o JSON do ORDS.
// ORDS APEX devolve campos em snake_case.
public class UsuarioOrdsResponse
{
    [JsonPropertyName("rm")]
    public string? Rm { get; set; }

    [JsonPropertyName("nome_completo")]
    public string? NomeCompleto { get; set; }

    [JsonPropertyName("email_institucional")]
    public string? EmailInstitucional { get; set; }
}

// Wrapper usado quando o ORDS encapsula a resposta em { items: [...] }
public class OrdsItemsWrapper<T>
{
    [JsonPropertyName("items")]
    public List<T>? Items { get; set; }
}