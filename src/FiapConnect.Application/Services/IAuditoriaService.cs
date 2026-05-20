using FiapConnect.Application.DTOs.Auditoria;

namespace FiapConnect.Application.Services;

// Auditoria so eh exposta via leitura na API (sem POST publico).
// O metodo RegistrarInternoAsync existe para uso interno de outros services
// no futuro, sem exposicao via controller. Hoje nao eh chamado por ninguem,
// mas mantem o RegistrarAsync do repository com proposito definido.
public interface IAuditoriaService
{
    Task<AuditoriaResponse> ObterPorIdAsync(string id);

    Task<IEnumerable<AuditoriaResponse>> ListarAsync();

    Task<IEnumerable<AuditoriaResponse>> ListarPorTabelaAsync(string tabela);

    // Uso interno: chamado por outros services para registrar log de operacao.
    // Nao eh exposto via controller.
    Task RegistrarInternoAsync(
        string tabela,
        int idRegistro,
        string tipoOperacao,
        string rmUsuario,
        Dictionary<string, object>? dadosAntes,
        Dictionary<string, object>? dadosDepois);
}