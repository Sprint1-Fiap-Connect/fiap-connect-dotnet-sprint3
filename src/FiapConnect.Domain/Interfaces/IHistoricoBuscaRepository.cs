using FiapConnect.Domain.Entities;

namespace FiapConnect.Domain.Interfaces;

public interface IHistoricoBuscaRepository
{
    Task<HistoricoBusca?> ObterPorIdAsync(string id);

    Task<IEnumerable<HistoricoBusca>> ListarPorAlunoAsync(string rmAluno);

    Task<HistoricoBusca> RegistrarAsync(HistoricoBusca historico);

    Task RemoverAsync(string id);
}