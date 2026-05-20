namespace FiapConnect.Application.Interfaces;

// Validar tokens de autenticacao emitidos pelo Firebase Auth
// A implementacao concreta FirebaseAuthClient fica no Infrastructure usando
// o pacote FirebaseAdmin. O fluxo eh: Mobile faz login no Firebase, recebe
// o idToken, envia para o AuthController do .NET, que chama este metodo
// para validar e extrair o email. O RM eh derivado do email (ex:
// "rm560384@fiap.com.br" -> "560384") no AuthService.
public interface IFirebaseAuthClient
{
    // Valida o idToken junto ao Firebase. Retorna o email do usuario se o
    // token for valido, ou null se for invalido/expirado.
    Task<string?> ValidarTokenERetornarEmailAsync(string idToken);
}