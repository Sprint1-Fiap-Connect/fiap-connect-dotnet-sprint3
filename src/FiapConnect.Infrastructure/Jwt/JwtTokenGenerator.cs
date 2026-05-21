using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FiapConnect.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FiapConnect.Infrastructure.Jwt;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraEm) Gerar(string rm, string email)
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expirationHoursStr = _configuration["Jwt:ExpirationHours"];

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Jwt:SecretKey nao configurada");
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("Jwt:Issuer nao configurado");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Jwt:Audience nao configurada");

        // HmacSha256 exige chave de no minimo 32 bytes
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey precisa de pelo menos 32 bytes para HmacSha256");
        }

        // Fallback de 8h quando ExpirationHours nao estiver configurado ou for invalido
        var expirationHours = int.TryParse(expirationHoursStr, out var h) ? h : 8;
        var expiraEm = DateTime.UtcNow.AddHours(expirationHours);

        // Sub = identificador do usuario (RM), Jti = identificador unico do token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, rm),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiraEm);
    }
}