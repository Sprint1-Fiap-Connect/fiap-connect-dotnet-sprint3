using FiapConnect.Application.DTOs.Conversa;
using FiapConnect.Application.Interfaces;
using FiapConnect.Application.Services;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;
using Moq;

namespace FiapConnect.UnitTests.Application.Services;

public class ConversaServiceTests
{
    [Fact]
    public async Task CriarAsync_QuandoRequestNull_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);

        // Act
        Func<Task> acao = () => service.CriarAsync(null!);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task CriarAsync_QuandoParticipantesContemRmsIguais_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "560384", "560384" }
        };

        // Act
        Func<Task> acao = () => service.CriarAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task CriarAsync_QuandoQuantidadeDeParticipantesDiferenteDeDois_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "560384" }
        };

        // Act
        Func<Task> acao = () => service.CriarAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task CriarAsync_QuandoConversaJaExisteEntreOsRms_RetornaExistenteSemChamarOracle()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        var existente = new Conversa
        {
            Id = "abc",
            IdConversa = "111111_560384",
            Participantes = new List<string> { "111111", "560384" },
            NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
            StatusConversa = "ATIVA"
        };
        repositoryMock
            .Setup(r => r.ObterEntreParticipantesAsync("560384", "111111"))
            .ReturnsAsync(existente);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "560384", "111111" }
        };

        // Act
        await service.CriarAsync(request);

        // Assert
        oracleMock.Verify(o => o.ObterUsuarioPorRmAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_QuandoUmDosRmsNaoExisteNoOracle_LancaRecursoNaoEncontradoException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterEntreParticipantesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Conversa?)null);
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("560384"))
            .ReturnsAsync(new Usuario { Rm = "560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("999999"))
            .ReturnsAsync((Usuario?)null);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "560384", "999999" }
        };

        // Act
        Func<Task> acao = () => service.CriarAsync(request);

        // Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(acao);
    }

    [Fact]
    public async Task CriarAsync_QuandoCriacaoBemSucedida_GeraIdConversaDeterministicoOrdenado()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterEntreParticipantesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Conversa?)null);
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("560384"))
            .ReturnsAsync(new Usuario { Rm = "560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("111111"))
            .ReturnsAsync(new Usuario { Rm = "111111", NomeCompleto = "Beatriz", EmailInstitucional = "rm111111@fiap.com.br" });
        repositoryMock
            .Setup(r => r.CriarAsync(It.IsAny<Conversa>()))
            .ReturnsAsync((Conversa c) => c);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "560384", "111111" }
        };

        // Act
        await service.CriarAsync(request);

        // Assert
        repositoryMock.Verify(r => r.CriarAsync(It.Is<Conversa>(c => c.IdConversa == "111111_560384")), Times.Once);
    }

    [Fact]
    public async Task EnviarMensagemAsync_QuandoRmRemetenteNaoEhParticipante_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterPorIdAsync("111111_560384"))
            .ReturnsAsync(new Conversa
            {
                Id = "abc",
                IdConversa = "111111_560384",
                Participantes = new List<string> { "111111", "560384" },
                NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
                StatusConversa = "ATIVA"
            });

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RmRemetente = "999999",
            Texto = "Oi"
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("111111_560384", request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task EnviarMensagemAsync_QuandoStatusDaConversaNaoEhAtiva_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterPorIdAsync("111111_560384"))
            .ReturnsAsync(new Conversa
            {
                Id = "abc",
                IdConversa = "111111_560384",
                Participantes = new List<string> { "111111", "560384" },
                NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
                StatusConversa = "ENCERRADA"
            });

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RmRemetente = "560384",
            Texto = "Oi"
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("111111_560384", request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task EnviarMensagemAsync_QuandoTextoVazio_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RmRemetente = "560384",
            Texto = string.Empty
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("111111_560384", request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task EnviarMensagemAsync_QuandoDadosValidos_DelegaAdicionarMensagemAoRepository()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterPorIdAsync("111111_560384"))
            .ReturnsAsync(new Conversa
            {
                Id = "abc",
                IdConversa = "111111_560384",
                Participantes = new List<string> { "111111", "560384" },
                NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
                StatusConversa = "ATIVA"
            });

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RmRemetente = "560384",
            Texto = "Mensagem teste"
        };

        // Act
        await service.EnviarMensagemAsync("111111_560384", request);

        // Assert
        repositoryMock.Verify(
            r => r.AdicionarMensagemAsync(
                "111111_560384",
                It.Is<MensagemItem>(m => m.RemetenteRm == "560384" && m.Texto == "Mensagem teste")),
            Times.Once);
    }
}