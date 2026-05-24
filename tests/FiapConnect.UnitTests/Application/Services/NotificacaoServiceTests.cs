using FiapConnect.Application.DTOs.Notificacao;
using FiapConnect.Application.Interfaces;
using FiapConnect.Application.Services;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using FiapConnect.Domain.Interfaces;
using Moq;

namespace FiapConnect.UnitTests.Application.Services;

public class NotificacaoServiceTests
{
    [Fact]
    public async Task CriarAsync_QuandoDestinatarioNaoExisteNoOracle_LancaRecursoNaoEncontradoException()
    {
        // Arrange
        var repositoryMock = new Mock<INotificacaoRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM999999"))
            .ReturnsAsync((Usuario?)null);

        var service = new NotificacaoService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "RM999999",
            Tipo = "INFO",
            Titulo = "Teste",
            Mensagem = "Mensagem teste"
        };

        // Act
        Func<Task> acao = () => service.CriarAsync(request);

        // Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(acao);
    }

    [Fact]
    public async Task CriarAsync_QuandoDadosValidos_GravaNotificacaoComOrigemDotnet()
    {
        // Arrange
        var repositoryMock = new Mock<INotificacaoRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        repositoryMock
            .Setup(r => r.CriarAsync(It.IsAny<Notificacao>()))
            .ReturnsAsync((Notificacao n) => n);

        var service = new NotificacaoService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "RM560384",
            Tipo = "INFO",
            Titulo = "Teste",
            Mensagem = "Mensagem teste",
            Prioridade = "NORMAL"
        };

        // Act
        var resultado = await service.CriarAsync(request);

        // Assert
        Assert.Equal("DOTNET", resultado.Origem);
        repositoryMock.Verify(r => r.CriarAsync(It.Is<Notificacao>(n => n.Origem == "DOTNET")), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_QuandoPrioridadeVazia_DefineComoNormal()
    {
        // Arrange
        var repositoryMock = new Mock<INotificacaoRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        repositoryMock
            .Setup(r => r.CriarAsync(It.IsAny<Notificacao>()))
            .ReturnsAsync((Notificacao n) => n);

        var service = new NotificacaoService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "RM560384",
            Tipo = "INFO",
            Titulo = "Teste",
            Mensagem = "Mensagem teste",
            Prioridade = string.Empty
        };

        // Act
        await service.CriarAsync(request);

        // Assert
        repositoryMock.Verify(r => r.CriarAsync(It.Is<Notificacao>(n => n.Prioridade == "NORMAL")), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_QuandoRmDestinatarioSemPrefixo_CanonizaParaRmMaiusculo()
    {
        // Arrange
        // Aceita "560384" (somente digitos) e canoniza pra "RM560384" antes de validar no Oracle
        var repositoryMock = new Mock<INotificacaoRepository>();
        var oracleMock = new Mock<IOracleClient>();
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("RM560384"))
            .ReturnsAsync(new Usuario { Rm = "RM560384", NomeCompleto = "Alexis", EmailInstitucional = "rm560384@fiap.com.br" });
        repositoryMock
            .Setup(r => r.CriarAsync(It.IsAny<Notificacao>()))
            .ReturnsAsync((Notificacao n) => n);

        var service = new NotificacaoService(repositoryMock.Object, oracleMock.Object);
        var request = new CriarNotificacaoRequest
        {
            RmDestinatario = "560384",
            Tipo = "INFO",
            Titulo = "Teste",
            Mensagem = "Mensagem teste"
        };

        // Act
        await service.CriarAsync(request);

        // Assert
        repositoryMock.Verify(
            r => r.CriarAsync(It.Is<Notificacao>(n => n.RmDestinatario == "RM560384")),
            Times.Once);
    }

    [Fact]
    public async Task MarcarComoLidaAsync_QuandoIdNaoExiste_LancaRecursoNaoEncontradoException()
    {
        // Arrange
        var repositoryMock = new Mock<INotificacaoRepository>();
        var oracleMock = new Mock<IOracleClient>();
        repositoryMock
            .Setup(r => r.ObterPorIdAsync("inexistente"))
            .ReturnsAsync((Notificacao?)null);

        var service = new NotificacaoService(repositoryMock.Object, oracleMock.Object);

        // Act
        Func<Task> acao = () => service.MarcarComoLidaAsync("inexistente");

        // Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(acao);
    }
}