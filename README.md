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
| Hospedagem | AWS EC2 (Docker Compose + Caddy + GitHub Actions) |

---

## Deploy

A API roda em uma instância **AWS EC2 t4g.small (ARM64)** orquestrada via
`docker-compose`, com 3 containers:

- `fiap-connect-api` — esta API .NET
- `oracle-proxy` — proxy Python que repassa chamadas ao ORDS
- `caddy` — reverse proxy com terminação TLS automática via Let's Encrypt

O pipeline CI/CD usa **GitHub Actions** (`.github/workflows/deploy.yml`):
build cross-arch (`docker buildx` com QEMU amd64 → arm64), SCP das imagens
e configs pra EC2 via SSH, restart do docker-compose, e smoke test
final validando `/health` retornando "Healthy".

Cada push na branch `main` dispara o pipeline. Tempo médio de deploy: ~6 min.

---

## Limitações conhecidas

- **JWT expira em 8 horas** por padrão. Configurável via
  `Jwt:ExpirationHours` no `appsettings`.
- **WAF Akamai do oracleapex.com** pode endurecer regras sem aviso.
  Em produção, a API consome o ORDS através de um proxy Python
  intermediário (container `oracle-proxy/` no docker-compose) que
  contorna o WAF. Em desenvolvimento local, a API aponta direto pra
  `oracleapex.com` usando os headers Chrome 126 configurados no DI.

---

## Links

- **API em produção (HTTPS):** https://44-214-247-152.sslip.io
- **Swagger:** https://44-214-247-152.sslip.io/swagger
- **Health Check:** https://44-214-247-152.sslip.io/health
- **Vídeo .NET :** Pendente
- **Repositório:** https://github.com/Sprint1-Fiap-Connect/fiap-connect-dotnet-sprint3