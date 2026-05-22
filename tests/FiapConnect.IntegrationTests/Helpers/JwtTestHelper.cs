using FiapConnect.Application.Interfaces;
using FiapConnect.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace FiapConnect.IntegrationTests.Helpers;

// Helper estatico para gerar JWT valido nos testes de integracao
// usando o mesmo JwtTokenGenerator registrado na DI da API
public static class JwtTestHelper
{
    public static string GerarJwtParaTeste(WebAppFixture fixture, string rm)
    {
        using var scope = fixture.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var (token, _) = jwt.Gerar(rm, $"{rm.ToLower()}@fiap.com.br");
        return token;
    }
}