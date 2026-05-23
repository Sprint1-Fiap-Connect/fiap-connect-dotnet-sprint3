using FiapConnect.Domain.Entities;

namespace FiapConnect.Application.Interfaces;

// Consumir o Oracle APEX via ORDS (REST).
// A implementacao concreta OracleClient (HttpClient tipado) fica no Infrastructure
// O .NET nao expoe CRUD relacional proprio; consulta o ORDS apenas para
// validacao interna (existencia de usuario) e health check
public interface IOracleClient
{
    // Retorna o usuario se existir no Oracle, ou null caso contrario
    // Usado pelos services para validar RMs e preencher snapshots de nome
    Task<Usuario?> ObterUsuarioPorRmAsync(string rm);

    // Verifica se o ORDS esta respondendo. Usado pelo health check da API
    // A implementacao faz GET em /fiapconnect/usuario/RM560384 (RM real e estavel)
    Task<bool> EstaSaudavelAsync();

    // Faz um GET bruto com a URL relativa informada e devolve o HttpResponseMessage
    // intacto. Usado pelo DebugController para inspecionar status, headers e body
    // exatamente como chegam do ORDS, confirmando que os 9 headers Akamai saem
    // corretamente do IP do Railway
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
}