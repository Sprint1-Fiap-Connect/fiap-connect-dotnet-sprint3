using System.Net.Http.Headers;
using FiapConnect.Application.Interfaces;
using FiapConnect.Application.Services;
using FiapConnect.Domain.Interfaces;
using FiapConnect.Infrastructure.Firebase;
using FiapConnect.Infrastructure.Jwt;
using FiapConnect.Infrastructure.Mongo;
using FiapConnect.Infrastructure.Mongo.Mapeamentos;
using FiapConnect.Infrastructure.Mongo.Repositories;
using FiapConnect.Infrastructure.Oracle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Mapeamentos Mongo: registrar antes do primeiro acesso ao driver
        ConversaMap.Registrar();
        NotificacaoMap.Registrar();
        HistoricoBuscaMap.Registrar();
        AuditoriaMap.Registrar();

        // Mongo
        services.AddSingleton<MongoContext>();
        services.AddScoped<IConversaRepository, ConversaRepository>();
        services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
        services.AddScoped<IHistoricoBuscaRepository, HistoricoBuscaRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        // Services da Application (consumidos pelos Controllers da API)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConversaService, ConversaService>();
        services.AddScoped<INotificacaoService, NotificacaoService>();
        services.AddScoped<IHistoricoBuscaService, HistoricoBuscaService>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        // Firebase: Singleton porque a inicializacao do FirebaseApp eh global ao processo
        services.AddSingleton<IFirebaseAuthClient, FirebaseAuthClient>();

        // JWT
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Oracle: HttpClient tipado com headers Mozilla pra burlar o WAF Akamai
        var ordsBaseUrl = configuration["Oracle:BaseUrl"]
            ?? throw new InvalidOperationException("Oracle:BaseUrl nao configurada");

        services.AddHttpClient<IOracleClient, OracleClient>(client =>
        {
            client.BaseAddress = new Uri(ordsBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.DefaultRequestHeaders.Add("Origin", "https://oracleapex.com");
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}