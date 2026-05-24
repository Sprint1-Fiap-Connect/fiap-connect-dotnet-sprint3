#!/usr/bin/env bash
# Habilita HTTPS no deploy existente:
#   1. Aloca Elastic IP e atacha na EC2 (IP fixo)
#   2. Abre portas 80 + 443 no Security Group
#   3. Computa DOMAIN via sslip.io (DNS gratuito que resolve qualquer IP)
#   4. Atualiza .env + envia docker-compose.yml + Caddyfile pro EC2
#   5. Reinicia compose; Caddy provisiona cert do Let's Encrypt automaticamente
#
# Pre-requisito: deploy.sh ja rodou com sucesso (EC2 ja existe, .aws-state populado)

set -euo pipefail
cd "$(dirname "$0")"
source ./vars.sh

say() { echo -e "\n\033[1;36m▶ $*\033[0m"; }
ok()  { echo -e "  \033[1;32m✓\033[0m $*"; }
warn(){ echo -e "  \033[1;33m!\033[0m $*"; }
err() { echo -e "  \033[1;31m✗\033[0m $*" >&2; }

save_state() {
  local key="$1" val="$2"
  grep -v "^${key}=" "$STATE_FILE" 2>/dev/null > "${STATE_FILE}.tmp" || true
  mv "${STATE_FILE}.tmp" "$STATE_FILE"
  echo "${key}=${val}" >> "$STATE_FILE"
}

[ -f "$STATE_FILE" ] || { err "$STATE_FILE nao existe. Rode deploy.sh primeiro."; exit 1; }
# shellcheck disable=SC1090
source "$STATE_FILE"

[ -n "${INSTANCE_ID:-}" ] || { err "INSTANCE_ID nao encontrado no state"; exit 1; }
[ -n "${SG_ID:-}" ]       || { err "SG_ID nao encontrado no state"; exit 1; }

# === 1. Elastic IP ======================================================
if [ -n "${EIP_ALLOC_ID:-}" ]; then
  warn "EIP ${EIP_ADDR:-?} ja alocado (state: $EIP_ALLOC_ID) - pulando alocacao"
else
  say "1/5 Alocando Elastic IP"
  EIP_ALLOC_ID=$(aws ec2 allocate-address \
    --profile "$AWS_PROFILE" --region "$AWS_REGION" \
    --domain vpc \
    --tag-specifications "ResourceType=elastic-ip,Tags=[{Key=Name,Value=${EIP_NAME}},{Key=Project,Value=${PROJECT}},{Key=Owner,Value=davi.praxedes}]" \
    --query AllocationId --output text)
  save_state EIP_ALLOC_ID "$EIP_ALLOC_ID"

  EIP_ADDR=$(aws ec2 describe-addresses \
    --profile "$AWS_PROFILE" --region "$AWS_REGION" \
    --allocation-ids "$EIP_ALLOC_ID" \
    --query 'Addresses[0].PublicIp' --output text)
  save_state EIP_ADDR "$EIP_ADDR"
  ok "EIP alocado: $EIP_ADDR ($EIP_ALLOC_ID)"

  say "2/5 Atachando EIP na EC2 $INSTANCE_ID"
  EIP_ASSOC_ID=$(aws ec2 associate-address \
    --profile "$AWS_PROFILE" --region "$AWS_REGION" \
    --instance-id "$INSTANCE_ID" --allocation-id "$EIP_ALLOC_ID" \
    --query AssociationId --output text)
  save_state EIP_ASSOC_ID "$EIP_ASSOC_ID"
  # Atualiza PUBLIC_IP no state com o EIP novo
  save_state PUBLIC_IP "$EIP_ADDR"
  ok "EIP atachado, novo IP publico: $EIP_ADDR"
fi

# === 2. Abrir 80 + 443 no SG ===========================================
say "3/5 Abrindo portas 80 + 443 no security group $SG_ID"
for port in 80 443; do
  if aws ec2 authorize-security-group-ingress \
       --profile "$AWS_PROFILE" --region "$AWS_REGION" \
       --group-id "$SG_ID" --protocol tcp --port "$port" --cidr 0.0.0.0/0 \
       >/dev/null 2>&1; then
    ok "Porta $port liberada"
  else
    warn "Porta $port ja estava aberta (ou erro ignoravel)"
  fi
done

# === 3. Computar DOMAIN sslip.io =======================================
DOMAIN="${EIP_ADDR//./-}.sslip.io"
save_state DOMAIN "$DOMAIN"
ok "DOMAIN computado: $DOMAIN"

# === 4. Atualizar .env e enviar arquivos =================================
say "4/5 Atualizando .env local e enviando pro EC2"
grep -v '^DOMAIN=' .env > .env.tmp 2>/dev/null || true
echo "DOMAIN=$DOMAIN" >> .env.tmp
mv .env.tmp .env
ok ".env atualizado com DOMAIN=$DOMAIN"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -i $KEY_PATH"
scp $SSH_OPTS docker-compose.yml Caddyfile .env ec2-user@"$EIP_ADDR":~/app/ >/dev/null
ok "Arquivos enviados (docker-compose.yml, Caddyfile, .env)"

# === 5. Reiniciar compose ===============================================
say "5/5 Reiniciando docker compose no EC2 (Caddy vai provisionar cert)"
ssh $SSH_OPTS ec2-user@"$EIP_ADDR" bash -s <<'REMOTE'
set -euo pipefail
cd ~/app
docker compose --env-file .env down 2>/dev/null || true
docker compose --env-file .env up -d
sleep 3
docker compose ps
REMOTE
ok "compose subiu"

# === 6. Esperar cert ====================================================
say "Aguardando Let's Encrypt provisionar certificado (~30-60s)..."
HTTPS_OK=""
for i in {1..40}; do
  RESP=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "https://${DOMAIN}/health" 2>/dev/null || echo "000")
  if [ "$RESP" = "200" ]; then
    HTTPS_OK="yes"
    ok "Cert OK em $((i * 3))s"
    break
  fi
  sleep 3
done

if [ -z "$HTTPS_OK" ]; then
  warn "HTTPS ainda nao respondeu 200 - cert pode estar sendo provisionado"
  warn "Veja logs: ssh -i $KEY_PATH ec2-user@$EIP_ADDR 'docker logs caddy --tail 30'"
fi

# === Resumo =============================================================
say "HTTPS configurado"
cat <<RESUMO

  🟢 URLs:
    https://${DOMAIN}/swagger
    https://${DOMAIN}/health
    https://${DOMAIN}/api/Debug/ords

  HTTP ainda funciona (8080 pra debug direto, 80 redirect pra 443):
    http://${EIP_ADDR}:8080/health  (fallback direto na app)

  SSH:
    ssh -i ${KEY_PATH} ec2-user@${EIP_ADDR}

  Caddy logs (acompanhar provisao de cert):
    ssh -i ${KEY_PATH} ec2-user@${EIP_ADDR} 'docker logs -f caddy'

RESUMO
