using FiapConnect.Application.DTOs.Auth;
using FiapConnect.Application.Interfaces;
using FiapConnect.Domain.Exceptions;

namespace FiapConnect.Application.Services;

public class AuthService : IAuthService
{
    private readonly IFirebaseAuthClient _firebaseClient;
    private readonly IOracleClient _oracleClient;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public AuthService(
        IFirebaseAuthClient firebaseClient,
        IOracleClient oracleClient,
        IJwtTokenGenerator jwtGenerator)
    {
        _firebaseClient = firebaseClient;
        _oracleClient = oracleClient;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (request is null)
            throw new RegraDeNegocioException("Request nao informado");

        if (string.IsNullOrWhiteSpace(request.IdToken))
            throw new RegraDeNegocioException("IdToken nao informado");

        // Valida o IdToken no Firebase e retorna o email
        var email = await _firebaseClient.ValidarTokenERetornarEmailAsync(request.IdToken);

        if (string.IsNullOrWhiteSpace(email))
            throw new RegraDeNegocioException("IdToken invalido");

        // Extrai RM do email no padrao rm560384@fiap.com.br (sem prefixo "rm")
        var rm = ExtrairRmDoEmail(email);

        // Busca usuario no Oracle (RM sem prefixo, o OracleClient concreto adapta pra URL)
        var usuario = await _oracleClient.ObterUsuarioPorRmAsync(rm);

        if (usuario == null)
            throw new RecursoNaoEncontradoException(
                $"Usuario com RM {rm} nao encontrado no Oracle");

        var (token, expiraEm) = _jwtGenerator.Gerar(rm, usuario.EmailInstitucional);

        return new LoginResponse
        {
            Token = token,
            Rm = rm,
            NomeCompleto = usuario.NomeCompleto,
            EmailInstitucional = usuario.EmailInstitucional,
            ExpiraEm = expiraEm
        };
    }

    private static string ExtrairRmDoEmail(string email)
    {
        var arroba = email.IndexOf('@');
        if (arroba <= 2)
            throw new RegraDeNegocioException("Email fora do padrao rmXXXXXX@fiap.com.br");

        var prefixo = email.Substring(0, arroba).ToLowerInvariant();
        if (!prefixo.StartsWith("rm"))
            throw new RegraDeNegocioException("Email fora do padrao rmXXXXXX@fiap.com.br");

        var rm = prefixo.Substring(2);

        if (rm.Length < 5 || !rm.All(char.IsDigit))
            throw new RegraDeNegocioException("RM extraido do email invalido");

        return rm;
    }
}