namespace FiapConnect.Application.Interfaces;

// Gera JWT proprio da API a partir dos dados do usuario autenticado no Firebase
// A implementacao concreta JwtTokenGenerator fica no Infrastructure e le SecretKey,
// Issuer, Audience e ExpirationHours via IConfiguration
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiraEm) Gerar(string rm, string email);
}