using FiapConnect.Application.DTOs.Auth;
using FiapConnect.Application.Interfaces;
using FiapConnect.Application.Services;
using FiapConnect.Domain.Entities;
using FiapConnect.Domain.Exceptions;
using Moq;

namespace FiapConnect.UnitTests.Application.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_QuandoRequestNull_LancaRegraDeNegocioException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);

        // Act
        Func<Task> acao = () => service.LoginAsync(null!);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoIdTokenVazio_LancaRegraDeNegocioException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = string.Empty };

        // Act
        Func<Task> acao = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoFirebaseRejeitaToken_LancaRegraDeNegocioException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token-invalido"))
            .ReturnsAsync((string?)null);

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token-invalido" };

        // Act
        Func<Task> acao = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoEmailSemPrefixoRm_LancaRegraDeNegocioException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token"))
            .ReturnsAsync("alexis@fiap.com.br");

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token" };

        // Act
        Func<Task> acao = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoRmMenorQue5Digitos_LancaRegraDeNegocioException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token"))
            .ReturnsAsync("rm123@fiap.com.br");

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token" };

        // Act
        Func<Task> acao = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoRmContemLetras_LancaRegraDeNegocioException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token"))
            .ReturnsAsync("rm56038A@fiap.com.br");

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token" };

        // Act
        Func<Task> acao = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<RegraDeNegocioException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoRmValidoMasUsuarioNaoExisteNoOracle_LancaRecursoNaoEncontradoException()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token"))
            .ReturnsAsync("rm560384@fiap.com.br");
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("560384"))
            .ReturnsAsync((Usuario?)null);

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token" };

        // Act
        Func<Task> acao = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(acao);
    }

    [Fact]
    public async Task LoginAsync_QuandoTudoValido_RetornaLoginResponseComToken()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        var expiraEm = DateTime.UtcNow.AddHours(8);
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token"))
            .ReturnsAsync("rm560384@fiap.com.br");
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("560384"))
            .ReturnsAsync(new Usuario
            {
                Rm = "560384",
                NomeCompleto = "Alexis Rondo",
                EmailInstitucional = "rm560384@fiap.com.br"
            });
        jwtMock
            .Setup(j => j.Gerar("560384", "rm560384@fiap.com.br"))
            .Returns(("token-fake", expiraEm));

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token" };

        // Act
        var resultado = await service.LoginAsync(request);

        // Assert
        Assert.Equal("token-fake", resultado.Token);
        Assert.Equal("560384", resultado.Rm);
        Assert.Equal("Alexis Rondo", resultado.NomeCompleto);
    }

    [Fact]
    public async Task LoginAsync_QuandoTudoValido_GeradorRecebeRmSemPrefixoEEmailInstitucional()
    {
        // Arrange
        var firebaseMock = new Mock<IFirebaseAuthClient>();
        var oracleMock = new Mock<IOracleClient>();
        var jwtMock = new Mock<IJwtTokenGenerator>();
        firebaseMock
            .Setup(f => f.ValidarTokenERetornarEmailAsync("token"))
            .ReturnsAsync("rm560384@fiap.com.br");
        oracleMock
            .Setup(o => o.ObterUsuarioPorRmAsync("560384"))
            .ReturnsAsync(new Usuario
            {
                Rm = "560384",
                NomeCompleto = "Alexis Rondo",
                EmailInstitucional = "rm560384@fiap.com.br"
            });
        jwtMock
            .Setup(j => j.Gerar(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("token-fake", DateTime.UtcNow.AddHours(8)));

        var service = new AuthService(firebaseMock.Object, oracleMock.Object, jwtMock.Object);
        var request = new LoginRequest { IdToken = "token" };

        // Act
        await service.LoginAsync(request);

        // Assert
        jwtMock.Verify(j => j.Gerar("560384", "rm560384@fiap.com.br"), Times.Once);
    }
}