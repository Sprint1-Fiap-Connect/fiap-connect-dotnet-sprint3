# FIAP Connect — API .NET (Sprint 4)

API REST do projeto FIAP Connect, sistema de formação de grupos acadêmicos
desenvolvido para o Challenge Oracle FIAP. Esta API .NET é um dos componentes
da solução integrada (Mobile + APEX/ORDS + .NET + Flask).

## Integrantes

| Nome | RM | Turma |
|------|----|-------|
| Alexis Rondo | 560384 | 2TDSPS |
| Vinicius Rodrigues de Oliveira | 559611 | 2TDSPS |

---

## Histórico da arquitetura — Sprint 3 para Sprint 4

Na Sprint 3, esta API .NET implementava o domínio relacional completo do
FIAP Connect (Usuários, Grupos, Habilidades, Solicitações) sobre Oracle
relacional via Entity Framework Core.

A Sprint 4 do Challenge Oracle exige que o Oracle APEX seja parte essencial
da solução, não apenas banco de dados — ele deve processar dados e expor
lógica via ORDS. Para atender este requisito sem duplicar funcionalidades,
o domínio relacional foi migrado integralmente para Oracle APEX/ORDS, que
agora é responsável pelos Usuários, Grupos, Habilidades e Solicitações.

Esta API .NET foi refatorada na Sprint 4 para assumir um papel
complementar: ser responsável pelas 4 coleções MongoDB do projeto
(mensagens, notificações, histórico de buscas, auditoria) e pela
autenticação centralizada via Firebase + JWT próprio. Toda a lógica
relacional antiga (Controllers de Usuario/Grupo/Habilidade/Solicitacao
da Sprint 3) foi removida.

A motivação dessa divisão é que cada componente fique responsável pelo
que faz melhor: APEX trata o relacional rico e exposto pelo Challenge,
e a API .NET trata o domínio NoSQL (chat em tempo de uso, notificações
e auditoria) que se beneficia de MongoDB.

---

## Visão geral do projeto integrado

O FIAP Connect é composto pelos seguintes componentes que se conectam por HTTP:

```
+----------------+      +----------------------+
|                |----->| Oracle APEX / ORDS   |  (Usuários, Grupos,
|                |      | (dominio relacional) |   Habilidades,
|    Mobile      |      +----------------------+   Solicitações)
| (React Native) |
|                |      +----------------------+
|                |----->| API .NET 8           |  (Conversas, Notificações,
|                |      | (este repositorio)   |   Histórico, Auditoria,
+----------------+      |  + Auth JWT          |   Health Checks)
                        +----------------------+
                                  |
                                  v
                        +----------------------+
                        | MongoDB Atlas        |  (4 coleções NoSQL)
                        +----------------------+

+----------------+      +----------------------+
| APEX / Mobile  |----->| Flask (IoT/GenIA)    |  (Random Forest para
+----------------+      +----------------------+   compatibilidade de grupos)
```

### Divisão de responsabilidades

| Componente | Responsabilidade |
|---|---|
| **Oracle APEX / ORDS** | Domínio relacional (Usuários, Grupos, Habilidades, Solicitações) |
| **API .NET** (este repo) | 4 coleções MongoDB + Autenticação JWT + Health Checks |
| **Mobile (React Native)** | Cliente: consome tanto APEX quanto .NET |
| **Flask (IoT/GenIA)** | Modelo Random Forest para cálculo de compatibilidade |

---

## O que esta API entrega

- Autenticação via Firebase idToken com emissão de JWT próprio (HS256)
- CRUD das 4 coleções MongoDB (mensagens, notificações, histórico de
  buscas, auditoria)
- Validação de usuários contra Oracle APEX via ORDS
- HATEOAS nos endpoints de listagem (links self, previous, next)
- Health Checks de Mongo e ORDS no endpoint `/health`
- Tratamento global de exceções com payload padronizado
- Logging estruturado com Serilog (correlação por RequestId)
- Tracing distribuído com OpenTelemetry
- Documentação interativa via Swagger
- 30 testes unitários + 9 testes de integração

---

## Stack técnica

| Componente | Tecnologia |
|---|---|
| Linguagem | C# 12 / .NET 8 |
| Framework Web | ASP.NET Core 8 |
| Persistência NoSQL | MongoDB Atlas + MongoDB.Driver 2.25 |
| Persistência Relacional (consumo) | Oracle 19c via ORDS (HttpClient tipado) |
| Autenticação | Firebase Admin SDK + JWT (HS256) |
| Logging | Serilog estruturado |
| Tracing | OpenTelemetry |
| Documentação API | Swashbuckle.AspNetCore (Swagger / OpenAPI) |
| Testes Unitários | xUnit + Moq |
| Testes Integração | xUnit + Microsoft.AspNetCore.Mvc.Testing + FluentAssertions |
| Hospedagem | Render.com (buildpack .NET) |

---

## Limitações conhecidas

- **Render free tier dorme** após 15 minutos de ociosidade. O primeiro
  request depois da pausa demora de 30 a 60 segundos para acordar
  (cold start).
- **JWT expira em 8 horas** por padrão. Configurável via
  `Jwt:ExpirationHours` no `appsettings`.
- **WAF Akamai do oracleapex.com** pode endurecer regras sem aviso.
  Os headers HTTP do `OracleClient` foram calibrados em 22/05/2026
  para passar pelo filtro atual (User-Agent Chrome 126 completo,
  Sec-Fetch-*, Origin, Referer). Pode precisar de ajuste futuro se
  o WAF endurecer regras.

---

## Links

- **Deploy Render:** TBD — atualizado após Rodada 6.A
- **Vídeo .NET solo:** TBD — atualizado após gravação
- **Vídeo de integração final (todas as disciplinas):** TBD — atualizado
  após integração completa
- **Repositório:** https://github.com/Sprint1-Fiap-Connect/fiap-connect-dotnet-sprint3