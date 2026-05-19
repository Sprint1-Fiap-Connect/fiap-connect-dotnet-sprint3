using FiapConnect.Domain.Entities;

namespace FiapConnect.Domain.Interfaces;

public interface IAuditoriaRepository
{
    Task<Auditoria?> ObterPorIdAsync(string id);

    Task<IEnumerable<Auditoria>> ListarAsync();

    Task<IEnumerable<Auditoria>> ListarPorTabelaAsync(string tabela);

    Task<Auditoria> RegistrarAsync(Auditoria auditoria);
}