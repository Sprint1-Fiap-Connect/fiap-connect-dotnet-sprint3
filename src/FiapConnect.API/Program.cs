using System.Text;
using FiapConnect.API.HealthChecks;
using FiapConnect.API.Middlewares;
using FiapConnect.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: logger global da aplicacao com saida estruturada para console
// FromLogContext permite que o ASP.NET Core enriqueca os logs com RequestId
// (correlacao automatica entre logs da mesma requisicao)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FiapConnect.API")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {RequestId} {Message:lj}{NewLine}{Exception}");
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();

// Swagger com suporte a JWT Bearer no botao "Authorize"
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FIAP Connect API",
        Version = "v1"
    });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Autenticacao JWT Bearer: valida o token emitido pelo JwtTokenGenerator
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// Health checks de Mongo e ORDS. Timeout individual evita /health pendurar
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>("mongo", timeout: TimeSpan.FromSeconds(5))
    .AddCheck<OracleHealthCheck>("ords", timeout: TimeSpan.FromSeconds(5));

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger habilitado sempre (necessario pra demo em producao no Render)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Logging estruturado de cada request (metodo, path, status, duration)
// Vem antes do middleware de excecoes para capturar tambem requests com erro
app.UseSerilogRequestLogging();

// Middleware global de excecoes
app.UseMiddleware<ExcecaoGlobalMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessario para WebApplicationFactory<Program> na FiapConnect.IntegrationTests
public partial class Program { }