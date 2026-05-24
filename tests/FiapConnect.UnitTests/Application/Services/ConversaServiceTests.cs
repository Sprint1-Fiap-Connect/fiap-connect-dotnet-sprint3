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
            Participantes = new List<string> { "RM560384", "RM560384" }
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
            Participantes = new List<string> { "RM560384" }
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
            IdConversa = "RM111111_RM560384",
            Participantes = new List<string> { "RM111111", "RM560384" },
            NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
            StatusConversa = "ATIVA"
        };
        repositoryMock
            .Setup(r => r.ObterEntreParticipantesAsync("RM560384", "RM111111"))
            .ReturnsAsync(existente);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "RM560384", "RM111111" }
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
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM999999"))
            .ReturnsAsync((Usuario?)null);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "RM560384", "RM999999" }
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
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM111111"))
            .ReturnsAsync(new Usuario { Rm = "RM111111", NomeCompleto = "Beatriz", EmailInstitucional = "rm111111@fiap.com.br" });
        repositoryMock
            .Setup(r => r.CriarAsync(It.IsAny<Conversa>()))
            .ReturnsAsync((Conversa c) => c);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "RM560384", "RM111111" }
        };

        // Act
        await service.CriarAsync(request);

        // Assert
        repositoryMock.Verify(r => r.CriarAsync(It.Is<Conversa>(c => c.IdConversa == "RM111111_RM560384")), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_QuandoRmSemPrefixoEhCanonizado_GeraIdConversaCorreto()
    {
        // Arrange
        // Aceita variacoes razoaveis (sem prefixo, lowercase) e canoniza para "RM" + digitos
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterEntreParticipantesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Conversa?)null);
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM111111"))
            .ReturnsAsync(new Usuario { Rm = "RM111111", NomeCompleto = "Beatriz", EmailInstitucional = "rm111111@fiap.com.br" });
        repositoryMock
            .Setup(r => r.CriarAsync(It.IsAny<Conversa>()))
            .ReturnsAsync((Conversa c) => c);

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarConversaRequest
        {
            ContextoGrupoId = 12,
            Participantes = new List<string> { "560384", "rm111111" }
        };

        // Act
        await service.CriarAsync(request);

        // Assert
        repositoryMock.Verify(r => r.CriarAsync(It.Is<Conversa>(c => c.IdConversa == "RM111111_RM560384")), Times.Once);
    }

    [Fact]
    public async Task EnviarMensagemAsync_QuandoRemetenteRmNaoehParticipante_LancaRegraDeNegocioException()
    {
        // Arrange
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterPorIdAsync("RM111111_RM560384"))
            .ReturnsAsync(new Conversa
            {
                Id = "abc",
                IdConversa = "RM111111_RM560384",
                Participantes = new List<string> { "RM111111", "RM560384" },
                NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
                StatusConversa = "ATIVA"
            });

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RemetenteRm = "RM999999",
            Texto = "Oi"
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("RM111111_RM560384", request);

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
            .Setup(r => r.ObterPorIdAsync("RM111111_RM560384"))
            .ReturnsAsync(new Conversa
            {
                Id = "abc",
                IdConversa = "RM111111_RM560384",
                Participantes = new List<string> { "RM111111", "RM560384" },
                NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
                StatusConversa = "ENCERRADA"
            });

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RemetenteRm = "RM560384",
            Texto = "Oi"
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("RM111111_RM560384", request);

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
            RemetenteRm = "RM560384",
            Texto = string.Empty
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("RM111111_RM560384", request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task EnviarMensagemAsync_QuandoRemetenteRmVazio_LancaRegraDeNegocioException()
    {
        // Arrange
        // Validacao explicita pra evitar o sintoma do bug 7.0 (mensagem "RM  nao eh participante")
        var repositoryMock = new Mock<IConversaRepository>();
        var oracleMock = new Mock<IOracleClient>();
        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RemetenteRm = string.Empty,
            Texto = "Oi"
        };

        // Act
        Func<Task> acao = () => service.EnviarMensagemAsync("RM111111_RM560384", request);

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
            .Setup(r => r.ObterPorIdAsync("RM111111_RM560384"))
            .ReturnsAsync(new Conversa
            {
                Id = "abc",
                IdConversa = "RM111111_RM560384",
                Participantes = new List<string> { "RM111111", "RM560384" },
                NomesParticipantes = new List<string> { "Beatriz", "Alexis" },
                StatusConversa = "ATIVA"
            });

        var service = new ConversaService(repositoryMock.Object, oracleMock.Object);
        var request = new EnviarMensagemRequest
        {
            RemetenteRm = "RM560384",
            Texto = "Mensagem teste"
        };

        // Act
        await service.EnviarMensagemAsync("RM111111_RM560384", request);

        // Assert
        repositoryMock.Verify(
            r => r.AdicionarMensagemAsync(
                "RM111111_RM560384",
                It.Is<MensagemItem>(m => m.RemetenteRm == "RM560384" && m.Texto == "Mensagem teste")),
            Times.Once);
    }
}