# Deploy AWS — FiapConnect

Esta pasta contém tudo necessário pra subir a app na AWS EC2 (ARM, t4g.small)
com Docker Compose e HTTPS automático via Caddy + Let's Encrypt.

## Arquitetura

```
                            INTERNET (HTTPS 443)
                                    │
                                    ▼
                      ┌─────────────────────────┐
                      │  EC2 t4g.small (ARM)    │
                      │  Elastic IP fixo        │
                      │                         │
                      │   caddy ─→ fiap-api ─→ oracle-proxy
                      │     :443    :8080         :9000
                      └─────────────┬───────────┘
                                    │
                                    ▼
                       APEX (oracleapex.com via WAF Akamai)
                       MongoDB Atlas
                       Firebase Auth
```

O `oracle-proxy` (Python + `curl_cffi`) impersona o TLS fingerprint do Chrome 124,
única forma de passar pelo WAF da Akamai que protege o APEX hospedado.

## Estrutura

| Arquivo | Pra que serve |
|---|---|
| `vars.sh` | Configs (project name, regiao, instance type, CIDRs) |
| `user-data.sh` | Cloud-init que instala Docker + Compose v2 na EC2 |
| `docker-compose.yml` | Define os 3 containers (caddy + fiap-api + oracle-proxy) |
| `Caddyfile` | Configura Caddy: TLS auto + reverse proxy pra fiap-api:8080 |
| `deploy.sh` | Cria toda a infra AWS (VPC + EC2 + SG) e sobe a app |
| `setup-https.sh` | Aloca EIP, abre portas 80/443, computa DOMAIN sslip.io e habilita HTTPS |
| `teardown.sh` | Destroi tudo na AWS (lê `.aws-state` pra saber o que apagar) |

## Como subir do zero (primeira vez)

Requisitos locais:
- AWS CLI v2 configurada (profile `davi-pessoal` ou similar — ajuste em `vars.sh`)
- Docker Desktop com buildx
- `~/secrets/serviceAccountKey.json` (service account do Firebase)
- `deploy/.env` com `JWT_SECRET_KEY` e `MONGO_CONN`

```bash
cd deploy/
bash deploy.sh         # cria VPC + EC2 + sobe app (HTTP na porta 8080)
bash setup-https.sh    # adiciona EIP + Caddy + Let's Encrypt
```

URL final: `https://<ip-com-dashes>.sslip.io` (ex: `https://44-214-247-152.sslip.io`)

## Como destruir

```bash
bash teardown.sh   # pede confirmacao 'yes' antes
```

## CI/CD

O workflow `.github/workflows/deploy.yml` faz deploy automático em **push pra main**.
Ele NÃO usa AWS CLI nem cria recursos — apenas builda imagens, faz `scp` pra EC2
e roda `docker compose up -d`. A infra base (criada por `deploy.sh` + `setup-https.sh`)
precisa existir antes.

### GitHub Secrets necessários

Configurar em `Settings → Secrets and variables → Actions`:

| Secret | Conteúdo |
|---|---|
| `EC2_HOST` | IP público da EC2 (ex: `44.214.247.152`) |
| `EC2_SSH_KEY` | Conteúdo completo do arquivo `.pem` (`-----BEGIN... -----END`) |
| `DOMAIN` | Domínio HTTPS (ex: `44-214-247-152.sslip.io`) |
| `JWT_SECRET_KEY` | Chave de assinatura do JWT |
| `MONGO_CONN` | Connection string do MongoDB Atlas |
| `FIREBASE_CREDENTIALS_JSON` | Conteúdo completo do `serviceAccountKey.json` |

## Custo

EC2 t4g.small está no AWS Free Tier (750h/mês por 12 meses).

| Estado | Custo |
|---|---|
| Rodando 24/7 dentro do Free Tier | $0 |
| Rodando 24/7 fora do Free Tier | ~$12/mês |
| Parado (`stop-instances`) | $0 de compute, EIP nao-atachado cobra $3.6/mês |
| Destruido (`teardown.sh`) | $0 |

## Segurança / TODOs

- [ ] SSH SG está aberto pra `0.0.0.0/0` (necessário pra GitHub Actions). Restrinjir
      quando migrar pra runner com IP fixo ou OIDC.
- [ ] Secrets ficam em GH Actions Secrets (criptografados). Considerar AWS Secrets
      Manager se quiser rotação automática.
- [ ] Firebase service account está sendo passado por env. Em produção, considere
      mover pra Secrets Manager / EBS criptografado.
