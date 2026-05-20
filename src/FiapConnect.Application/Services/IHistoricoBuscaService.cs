using FiapConnect.Application.DTOs.HistoricoBusca;

namespace FiapConnect.Application.Services;

public interface IHistoricoBuscaService
{
    Task<HistoricoBuscaResponse> RegistrarAsync(RegistrarBuscaRequest request);

    Task<HistoricoBuscaResponse> ObterPorIdAsync(string id);

    Task<IEnumerable<HistoricoBuscaResponse>> ListarPorAlunoAsync(string rmAluno);

    Task RemoverAsync(string id);
}