using FiapConnect.Application.DTOs.HistoricoBusca;
using FiapConnect.Application.Interfaces;
using FiapConnect.Application.Services;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;
using Moq;

namespace FiapConnect.UnitTests.Application.Services;

public class HistoricoBuscaServiceTests
{
    [Fact]
    public async Task RegistrarAsync_QuandoAlunoNaoExisteNoOracle_LancaRecursoNaoEncontradoException()
    {
        // Arrange
        var repositoryMock = new Mock<IHistoricoBuscaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM999999"))
            .ReturnsAsync((Usuario?)null);

        var service = new HistoricoBuscaService(repositoryMock.Object, oracleMock.Object);
        var request = new RegistrarBuscaRequest
        {
            RmAluno = "RM999999",
            FiltroDisciplina = "DOTNET",
            EdicaoChallenge = "ORACLE_2024_2"
        };

        // Act
        Func<Task> acao = () => service.RegistrarAsync(request);

        // Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(acao);
    }

    [Fact]
    public async Task RegistrarAsync_QuandoAlunoExiste_GravaComNomeAlunoDoOracle()
    {
        // Arrange
        var repositoryMock = new Mock<IHistoricoBuscaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis Rondo", EmailInstitucional = "rm560384@fiap.com.br" });
        repositoryMock
            .Setup(r => r.RegistrarAsync(It.IsAny<HistoricoBusca>()))
            .ReturnsAsync((HistoricoBusca h) => h);

        var service = new HistoricoBuscaService(repositoryMock.Object, oracleMock.Object);
        var request = new RegistrarBuscaRequest
        {
            RmAluno = "RM560384",
            FiltroDisciplina = "DOTNET",
            EdicaoChallenge = "ORACLE_2024_2"
        };

        // Act
        await service.RegistrarAsync(request);

        // Assert
        repositoryMock.Verify(r => r.RegistrarAsync(It.Is<HistoricoBusca>(h => h.NomeAluno == "Alexis Rondo")), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_QuandoChamado_CalculaTotalGruposRetornadosDoTamanhoDaLista()
    {
        // Arrange
        var repositoryMock = new Mock<IHistoricoBuscaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        repositoryMock
            .Setup(r => r.RegistrarAsync(It.IsAny<HistoricoBusca>()))
            .ReturnsAsync((HistoricoBusca h) => h);

        var service = new HistoricoBuscaService(repositoryMock.Object, oracleMock.Object);
        var request = new RegistrarBuscaRequest
        {
            RmAluno = "RM560384",
            FiltroDisciplina = "DOTNET",
            EdicaoChallenge = "ORACLE_2024_2",
            GruposRetornados = new List<GrupoRetornadoDto>
            {
                new() { IdGrupo = 1, NomeGrupo = "G1", Percentual = 100, ClassificacaoIa = "ALTA" },
                new() { IdGrupo = 2, NomeGrupo = "G2", Percentual = 75, ClassificacaoIa = "MEDIA" },
                new() { IdGrupo = 3, NomeGrupo = "G3", Percentual = 50, ClassificacaoIa = "BAIXA" }
            }
        };

        // Act
        await service.RegistrarAsync(request);

        // Assert
        repositoryMock.Verify(r => r.RegistrarAsync(It.Is<HistoricoBusca>(h => h.TotalGruposRetornados == 3)), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_QuandoRmAlunoSemPrefixo_CanonizaParaRmMaiusculo()
    {
        // Arrange
        // Aceita "rm560384" (lowercase) e canoniza pra "RM560384" antes de validar no Oracle
        var repositoryMock = new Mock<IHistoricoBuscaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        repositoryMock
            .Setup(r => r.RegistrarAsync(It.IsAny<HistoricoBusca>()))
            .ReturnsAsync((HistoricoBusca h) => h);

        var service = new HistoricoBuscaService(repositoryMock.Object, oracleMock.Object);
        var request = new RegistrarBuscaRequest
        {
            RmAluno = "rm560384",
            FiltroDisciplina = "DOTNET",
            EdicaoChallenge = "ORACLE_2024_2"
        };

        // Act
        await service.RegistrarAsync(request);

        // Assert
        repositoryMock.Verify(
            r => r.RegistrarAsync(It.Is<HistoricoBusca>(h => h.RmAluno == "RM560384")),
            Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoIdNaoExiste_LancaRecursoNaoEncontradoException()
    {
        // Arrange
        var repositoryMock = new Mock<IHistoricoBuscaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterPorIdAsync("inexistente"))
            .ReturnsAsync((HistoricoBusca?)null);

        var service = new HistoricoBuscaService(repositoryMock.Object, oracleMock.Object);

        // Act
        Func<Task> acao = () => service.RemoverAsync("inexistente");

        // Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(acao);
    }
}