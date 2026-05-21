using FiapConnect.Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace FiapConnect.Infrastructure.Firebase;

public class FirebaseAuthClient : IFirebaseAuthClient
{
    // Inicializacao do FirebaseApp eh global por processo.
    // Lock + flag estatica garantem que rode UMA unica vez mesmo com varias instancias do client.
    private static readonly object _lock = new();
    private static bool _initialized = false;

    public FirebaseAuthClient(IConfiguration configuration)
    {
        lock (_lock)
        {
            if (_initialized) return;

            // Se outra parte do processo ja inicializou, marca como pronto e segue
            if (FirebaseApp.DefaultInstance != null)
            {
                _initialized = true;
                return;
            }

            var credentialsPath = configuration["Firebase:CredentialsPath"];
            var credentialsBase64 = configuration["Firebase:CredentialsBase64"];

            GoogleCredential credential;

            if (!string.IsNullOrWhiteSpace(credentialsBase64))
            {
                // Producao (Render): credencial vem em base64 via variavel de ambiente
                var bytes = Convert.FromBase64String(credentialsBase64);
                using var stream = new MemoryStream(bytes);
                credential = GoogleCredential.FromStream(stream);
            }
            else if (!string.IsNullOrWhiteSpace(credentialsPath) && File.Exists(credentialsPath))
            {
                // Dev local: arquivo serviceAccountKey.json no disco
                credential = GoogleCredential.FromFile(credentialsPath);
            }
            else
            {
                throw new InvalidOperationException(
                    "Credenciais Firebase nao configuradas (nem Firebase:CredentialsBase64 nem Firebase:CredentialsPath)");
            }

            FirebaseApp.Create(new AppOptions { Credential = credential });
            _initialized = true;
        }
    }

    public async Task<string?> ValidarTokenERetornarEmailAsync(string idToken)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

            if (decoded.Claims.TryGetValue("email", out var email))
            {
                return email?.ToString();
            }

            return null;
        }
        catch (FirebaseAuthException)
        {
            // Token invalido, expirado ou assinatura nao confere
            return null;
        }
    }
}