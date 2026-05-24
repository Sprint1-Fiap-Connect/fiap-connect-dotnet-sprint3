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

O FIAP Connect é composto por quatro componentes que se conectam por HTTP:
            ┌─────────────────────────────────────────────┐
            │                                             │
            │   Mobile (React Native + Expo + TanStack)   │
            │                                             │
            └──────────────┬───────────────┬──────────────┘
                           │               │
              HTTPS REST   │               │   HTTPS REST
                           │               │
                           ▼               ▼
   ┌─────────────────────────────┐   ┌──────────────────────────────┐
   │                             │   │                              │
   │   Oracle APEX / ORDS        │   │   API .NET 8 (este repo)     │
   │   (domínio relacional)      │   │   https://44-214-247-152     │
   │                             │   │      .sslip.io (AWS EC2)     │
   │   • Usuários                │   │                              │
   │   • Grupos                  │   │   • Auth (Firebase + JWT)    │
   │   • Habilidades             │   │   • CRUD 4 coleções Mongo    │
   │   • Solicitações            │   │   • HATEOAS + Paginação      │
   │                             │   │   • Health Checks            │
   └──────────────┬──────────────┘   └──────┬─────────────────┬─────┘
                  │                         │                 │
                  │  validação RM           │ MongoDB Driver  │ HTTP via
                  │  via proxy Python       │                 │ proxy Python
                  │                         ▼                 │
                  │              ┌────────────────────────┐   │
                  │              │   MongoDB Atlas        │   │
                  │              │   • mensagens          │   │
                  │              │   • notificacoes       │   │
                  │              │   • historico_buscas   │   │
                  │              │   • auditoria          │   │
                  │              └────────────────────────┘   │
                  │                                           │
                  └────────────────────┬──────────────────────┘
                                       │
                                       ▼
                              ┌──────────────────────┐
                              │   Flask (IoT/GenIA)  │
                              │   Random Forest      │
                              │   compatibilidade    │
                              └──────────────────────┘

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
- Canonização tolerante de RM (aceita `RM560384`, `rm560384` ou `560384`)
- HATEOAS nos endpoints de listagem (links self, previous, next)
- Paginação configurável via query string (`pagina`, `tamanhoPagina`)
- Health Checks de Mongo e ORDS no endpoint `/health`
- Tratamento global de exceções com payload padronizado (400, 404, 500)
- Logging estruturado com Serilog (correlação por RequestId)
- Tracing distribuído com OpenTelemetry
- Documentação interativa via Swagger / OpenAPI
- 47 testes unitários + 9 testes de integração (Padrão AAA com xUnit + Moq)

---

## Endpoints principais

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/auth/login` | Autentica com idToken Firebase e retorna JWT próprio |
| `POST` | `/api/conversas` | Cria conversa entre 2 RMs (idempotente) |
| `GET` | `/api/conversas/{id}` | Detalhes da conversa (aceita ObjectId ou IdConversa lógico) |
| `GET` | `/api/conversas?rm=` | Lista conversas de um participante (paginada) |
| `POST` | `/api/conversas/{id}/mensagens` | Envia mensagem na conversa |
| `PATCH` | `/api/conversas/{id}/mensagens/lidas` | Marca mensagens como lidas |
| `DELETE` | `/api/conversas/{id}` | Remove conversa |
| `POST` | `/api/notificacoes` | Cria notificação |
| `GET` | `/api/notificacoes/{id}` | Obtém notificação por ID |
| `GET` | `/api/notificacoes?rmDestinatario=` | Lista notificações por destinatário (paginada) |
| `PATCH` | `/api/notificacoes/{id}/lida` | Marca notificação como lida |
| `DELETE` | `/api/notificacoes/{id}` | Remove notificação |
| `POST` | `/api/historico-buscas` | Registra busca realizada pelo aluno |
| `GET` | `/api/historico-buscas/{id}` | Obtém histórico por ID |
| `GET` | `/api/historico-buscas?rmAluno=` | Lista histórico por aluno (paginada) |
| `DELETE` | `/api/historico-buscas/{id}` | Remove histórico |
| `GET` | `/api/auditoria/{id}` | Obtém registro de auditoria por ID |
| `GET` | `/api/auditoria?tabelaAfetada=` | Lista auditoria (paginada, filtro opcional) |
| `GET` | `/api/debug/ords` | Diagnóstico da integração com Oracle (autenticado) |
| `GET` | `/health` | Health Check público (Mongo + ORDS) |
| `GET` | `/swagger` | Documentação interativa |

Documentação completa e exemplos de payload disponíveis no Swagger.

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

## Mudanças relevantes da Sprint 4

Esta seção resume as decisões arquiteturais e técnicas tomadas durante
a Sprint 4 que impactam diretamente o uso e a integração da API.

### Arquitetura

- **Domínio relacional migrado para Oracle APEX/ORDS.** A API .NET deixou
  de operar tabelas relacionais e passou a consumir o ORDS via HttpClient.
- **Proxy Python intermediando o consumo do ORDS.** Necessário pra
  contornar o WAF Akamai do `oracleapex.com`, que bloqueia IPs de cloud
  pública. Container separado no mesmo `docker-compose`.
- **Persistência exclusiva em MongoDB Atlas.** As 4 coleções
  (`mensagens`, `notificacoes`, `historico_buscas`, `auditoria`) substituem
  a persistência relacional antiga.

### Comportamento da API

- **RM em formato canônico e tolerante.** Todos os endpoints que recebem
  RM aceitam `RM560384`, `rm560384` ou apenas `560384`. O service normaliza
  internamente para o formato canônico (`RM560384`) antes de validar no
  Oracle e persistir.
- **Conversas referenciadas por ObjectId ou IdConversa lógico.** O path id
  dos endpoints de Conversa aceita tanto o ObjectId hex do Mongo quanto
  o IdConversa lógico (`RMxxxxxx_RMyyyyyy`), que é determinístico e
  conhecido pelo cliente.
- **Path id inválido retorna 404, não 500.** Os repositories validam o
  formato do ObjectId antes de filtrar no Mongo, evitando que strings
  arbitrárias gerem erro genérico.
- **Rota de autenticação padronizada em lowercase** (`/api/auth/login`),
  consistente com os demais controllers.

### Persistência

- **`Dictionary<string, object>` suportado em campos dinâmicos.** Os
  campos `dadosContexto` (notificações) e `dadosAntes`/`dadosDepois`
  (auditoria) aceitam objetos JSON arbitrários, com tipos misturados
  (string, int, bool) e estrutura aninhada (objetos e arrays) preservada
  no BSON.
- **`JsonElement` normalizado antes de persistir.** Helper interno
  converte valores recebidos do ASP.NET para tipos primitivos
  equivalentes, evitando incompatibilidade com o driver Mongo.

### Qualidade

- **56 testes verdes** (47 unitários + 9 de integração).
- **Testes de integração com WebApplicationFactory** exercitando
  autenticação real (JWT), validação contra Oracle real (via proxy) e
  persistência real (MongoDB Atlas em base de teste isolada).

---

## Limitações conhecidas

- **JWT expira em 8 horas** por padrão. Configurável via
  `Jwt:ExpirationHours` no `appsettings`.
- **WAF Akamai do oracleapex.com** pode endurecer regras sem aviso.
  Em produção, a API consome o ORDS através de um proxy Python
  intermediário (container `oracle-proxy/` no docker-compose) que
  contorna o WAF. Em desenvolvimento local, a API aponta direto pra
  `oracleapex.com` usando os headers Chrome 126 configurados no DI.
- **Pipeline CI/CD não executa `dotnet test`** automaticamente, apenas
  smoke test do endpoint `/health`. A validação de testes é
  responsabilidade do desenvolvedor antes do push.

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

## Como testar localmente

A API pode ser baixada e validada localmente para inspeção do código e
execução dos testes unitários, que rodam sem nenhuma configuração
externa.

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### Passos

1. Clone o repositório:

```bash
git clone https://github.com/Sprint1-Fiap-Connect/fiap-connect-dotnet-sprint3.git
cd fiap-connect-dotnet-sprint3
```

2. Restaure os pacotes e compile a solução:

```bash
dotnet build FiapConnect.sln
```

Resultado esperado: build concluído sem erros.

3. Execute os testes unitários:

```bash
dotnet test tests/FiapConnect.UnitTests
```

Resultado esperado: **47 testes verdes**, cobrindo os 5 services da
camada Application (`AuthService`, `ConversaService`, `NotificacaoService`,
`HistoricoBuscaService`, `AuditoriaService`). Os testes usam Moq para
isolar dependências externas (MongoDB, Firebase, Oracle), portanto não
exigem credenciais nem rede.

4. (Opcional) Execute todos os testes incluindo integração:

```bash
dotnet test FiapConnect.sln
```

Resultado esperado: **56 testes** (47 unitários + 8 integração + 1
ignorado por exigir idToken Firebase real). Os testes de integração
exigem credenciais válidas em `appsettings.Test.json` (MongoDB Atlas,
Firebase, JWT secret) e acesso de rede.

### Observações

- Os **testes de integração** exigem credenciais de MongoDB Atlas e
  Firebase configuradas em `appsettings.Test.json` e usam o database
  `fiap_connect_test` (separado do `fiap_connect` de produção).

- A **API em si** (`dotnet run`) também exige essas credenciais para
  iniciar. Para avaliar a API funcionando, veja o video de integração
  na seção [Links](#links) — 

---

## Links

- **Vídeo de demonstração:** [https://youtu.be/3-qGD7G8NU4]    
     (Usando Swagger e com integração real com MongoDB)

- **Swagger:** https://44-214-247-152.sslip.io/swagger   
(não acessivel sem Jwt gerado por Firebase Auth via projeto mobile)

- **API em produção (HTTPS):** https://44-214-247-152.sslip.io
- **Health Check:** https://44-214-247-152.sslip.io/health
- **Repositório:** https://github.com/Sprint1-Fiap-Connect/fiap-connect-dotnet-sprint3