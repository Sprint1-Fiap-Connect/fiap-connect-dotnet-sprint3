namespace FiapConnect.Domain.Helpers;

// Centraliza a logica de canonizacao de RM para evitar inconsistencia.
// O sistema sempre opera com o formato "RM" + 6 digitos em caixa alta.
// Entradas validas: "560384", "rm560384", "Rm560384", "RM560384"
// Entrada invalida: qualquer string que nao se encaixe nesses padroes
public static class RmHelper
{
    // Recebe o RM em qualquer variacao razoavel e retorna a forma canonica.
    // Retorna null se o input nao puder ser canonizado
    public static string? Canonizar(string? rm)
    {
        if (string.IsNullOrWhiteSpace(rm))
            return null;

        var limpo = rm.Trim();

        // Se ja comeca com "rm" (case-insensitive), canoniza o prefixo
        if (limpo.Length >= 2 && limpo[..2].Equals("RM", StringComparison.OrdinalIgnoreCase))
        {
            var numero = limpo[2..];
            if (numero.Length >= 1 && numero.All(char.IsDigit))
                return "RM" + numero;
            return null;
        }

        // Se eh so digito, adiciona o prefixo
        if (limpo.All(char.IsDigit))
            return "RM" + limpo;

        return null;
    }
}