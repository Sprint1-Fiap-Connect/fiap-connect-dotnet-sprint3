using System.Net;
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FiapConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Permite que Dictionary<string, object> seja serializado pelo driver Mongo.
        // Sem esse registro, qualquer valor nao-null no dicionario causa excecao
        // de serializacao quando o documento eh inserido ou atualizado
        if (BsonSerializer.LookupSerializer(typeof(object)) is not ObjectSerializer)
        {
            BsonSerializer.RegisterSerializer(
                typeof(object),
                new ObjectSerializer(type => true));
        }

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

        // Oracle: HttpClient tipado com headers Chrome completos pra burlar o WAF Akamai
        var ordsBaseUrl = configuration["Oracle:BaseUrl"]
            ?? throw new InvalidOperationException("Oracle:BaseUrl nao configurada");

        services
            .AddHttpClient<IOracleClient, OracleClient>(client =>
            {
                client.BaseAddress = new Uri(ordsBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);

                // User-Agent completo do Chrome 126: WAF rejeita UAs simples
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                    "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/json, text/plain, */*");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(
                    "pt-BR,pt;q=0.9,en;q=0.8");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");

                client.DefaultRequestHeaders.Add("Origin", "https://oracleapex.com");
                client.DefaultRequestHeaders.Referrer = new Uri("https://oracleapex.com/");

                // Sec-Fetch-* simulam fetch CORS do navegador, sinais que o WAF observa
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Necessario porque enviamos Accept-Encoding gzip/deflate/br nos headers
                AutomaticDecompression = DecompressionMethods.GZip
                    | DecompressionMethods.Deflate
                    | DecompressionMethods.Brotli
            });

        return services;
    }
}