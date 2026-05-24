#!/usr/bin/env bash
# Deploy completo do FiapConnect na AWS EC2 (ARM, t4g.small).
# Cria VPC dedicada, EC2, instala Docker e sobe os 2 containers via docker-compose.
#
# Pre-requisitos:
#   - AWS CLI v2 + profile davi-pessoal configurado
#   - Docker (com buildx) rodando localmente
#   - ~/secrets/serviceAccountKey.json presente
#   - deploy/.env com JWT_SECRET_KEY e MONGO_CONN
#
# Uso: bash deploy.sh

set -euo pipefail
cd "$(dirname "$0")"
source ./vars.sh

# === Helpers ============================================================
say() { echo -e "\n\033[1;36m▶ $*\033[0m"; }
ok()  { echo -e "  \033[1;32m✓\033[0m $*"; }
warn(){ echo -e "  \033[1;33m!\033[0m $*"; }
err() { echo -e "  \033[1;31m✗\033[0m $*" >&2; }

# Salva chave=valor no STATE_FILE pra teardown.sh consumir
save_state() {
  local key="$1" val="$2"
  # Remove se ja existir, depois adiciona
  grep -v "^${key}=" "$STATE_FILE" 2>/dev/null > "${STATE_FILE}.tmp" || true
  mv "${STATE_FILE}.tmp" "$STATE_FILE"
  echo "${key}=${val}" >> "$STATE_FILE"
}

# === Sanity checks ======================================================
say "Sanity checks"
[ -f ".env" ]                                    || { err ".env nao encontrado em $(pwd)"; exit 1; }
[ -f "$HOME/secrets/serviceAccountKey.json" ]    || { err "serviceAccountKey.json nao encontrado"; exit 1; }
command -v aws >/dev/null                        || { err "aws cli nao instalada"; exit 1; }
command -v docker >/dev/null                     || { err "docker nao instalado"; exit 1; }
docker buildx version >/dev/null 2>&1            || { err "docker buildx nao disponivel"; exit 1; }

ACCOUNT=$(aws sts get-caller-identity --profile "$AWS_PROFILE" --query Account --output text)
[ "$ACCOUNT" = "581507488494" ] || { err "Profile $AWS_PROFILE aponta pra conta $ACCOUNT, esperado 581507488494"; exit 1; }
ok "AWS account: $ACCOUNT (davi-pessoal)"
ok "Regiao: $AWS_REGION"

mkdir -p "$HOME/secrets"
touch "$STATE_FILE"

# === 1. VPC =============================================================
say "1/8 Criando VPC $VPC_NAME ($VPC_CIDR)"
VPC_ID=$(aws ec2 create-vpc \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --cidr-block "$VPC_CIDR" \
  --tag-specifications "ResourceType=vpc,Tags=[{Key=Name,Value=$VPC_NAME},{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'Vpc.VpcId' --output text)
aws ec2 modify-vpc-attribute --profile "$AWS_PROFILE" --region "$AWS_REGION" --vpc-id "$VPC_ID" --enable-dns-hostnames
save_state VPC_ID "$VPC_ID"
ok "VPC criada: $VPC_ID"

# === 2. Internet Gateway ================================================
say "2/8 Criando Internet Gateway + attach na VPC"
IGW_ID=$(aws ec2 create-internet-gateway \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --tag-specifications "ResourceType=internet-gateway,Tags=[{Key=Name,Value=$IGW_NAME},{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'InternetGateway.InternetGatewayId' --output text)
save_state IGW_ID "$IGW_ID"
aws ec2 attach-internet-gateway --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --internet-gateway-id "$IGW_ID" --vpc-id "$VPC_ID"
ok "IGW: $IGW_ID (attached na VPC)"

# === 3. Subnet publica ==================================================
say "3/8 Criando subnet publica $SUBNET_CIDR em $AZ"
SUBNET_ID=$(aws ec2 create-subnet \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --vpc-id "$VPC_ID" --cidr-block "$SUBNET_CIDR" --availability-zone "$AZ" \
  --tag-specifications "ResourceType=subnet,Tags=[{Key=Name,Value=$SUBNET_NAME},{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'Subnet.SubnetId' --output text)
aws ec2 modify-subnet-attribute --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --subnet-id "$SUBNET_ID" --map-public-ip-on-launch
save_state SUBNET_ID "$SUBNET_ID"
ok "Subnet: $SUBNET_ID (auto-assign public IP habilitado)"

# === 4. Route Table =====================================================
say "4/8 Criando route table com rota default 0.0.0.0/0 → IGW"
RT_ID=$(aws ec2 create-route-table \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --vpc-id "$VPC_ID" \
  --tag-specifications "ResourceType=route-table,Tags=[{Key=Name,Value=$RT_NAME},{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'RouteTable.RouteTableId' --output text)
save_state RT_ID "$RT_ID"
aws ec2 create-route --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --route-table-id "$RT_ID" --destination-cidr-block 0.0.0.0/0 --gateway-id "$IGW_ID" >/dev/null
RT_ASSOC=$(aws ec2 associate-route-table --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --route-table-id "$RT_ID" --subnet-id "$SUBNET_ID" \
  --query 'AssociationId' --output text)
save_state RT_ASSOC "$RT_ASSOC"
ok "Route table $RT_ID associada na subnet"

# === 5. Security Group ==================================================
say "5/8 Criando security group (SSH 22 + API 8080)"
SG_ID=$(aws ec2 create-security-group \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --group-name "$SG_NAME" --description "FiapConnect lab" --vpc-id "$VPC_ID" \
  --tag-specifications "ResourceType=security-group,Tags=[{Key=Name,Value=$SG_NAME},{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'GroupId' --output text)
save_state SG_ID "$SG_ID"

# Detecta IP publico atual pra liberar SSH so do seu IP (seguranca minima)
MY_IP=$(curl -s https://checkip.amazonaws.com)
aws ec2 authorize-security-group-ingress --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --group-id "$SG_ID" --protocol tcp --port 22 --cidr "${MY_IP}/32" >/dev/null
aws ec2 authorize-security-group-ingress --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --group-id "$SG_ID" --protocol tcp --port 8080 --cidr 0.0.0.0/0 >/dev/null
ok "SG $SG_ID: SSH 22 liberado pra ${MY_IP}/32, API 8080 publica"

# === 6. Key Pair ========================================================
say "6/8 Criando key pair $KEY_NAME"
if aws ec2 describe-key-pairs --profile "$AWS_PROFILE" --region "$AWS_REGION" --key-names "$KEY_NAME" >/dev/null 2>&1; then
  warn "Key pair $KEY_NAME ja existe na AWS — vou deletar e recriar"
  aws ec2 delete-key-pair --profile "$AWS_PROFILE" --region "$AWS_REGION" --key-name "$KEY_NAME"
fi
aws ec2 create-key-pair \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --key-name "$KEY_NAME" \
  --tag-specifications "ResourceType=key-pair,Tags=[{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'KeyMaterial' --output text > "$KEY_PATH"
chmod 400 "$KEY_PATH"
save_state KEY_NAME "$KEY_NAME"
ok "Key pair salva em $KEY_PATH"

# === 7. EC2 Instance ====================================================
say "7/8 Lancando EC2 $INSTANCE_TYPE ($INSTANCE_ARCH) com Amazon Linux 2023"
AMI_ID=$(aws ssm get-parameters --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --names "$AMI_SSM_PARAM" --query 'Parameters[0].Value' --output text)
ok "AMI: $AMI_ID"

INSTANCE_ID=$(aws ec2 run-instances \
  --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --image-id "$AMI_ID" \
  --instance-type "$INSTANCE_TYPE" \
  --key-name "$KEY_NAME" \
  --subnet-id "$SUBNET_ID" \
  --security-group-ids "$SG_ID" \
  --user-data "file://user-data.sh" \
  --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=$INSTANCE_NAME},{Key=Project,Value=$PROJECT},{Key=Owner,Value=davi.praxedes}]" \
  --query 'Instances[0].InstanceId' --output text)
save_state INSTANCE_ID "$INSTANCE_ID"
ok "EC2 lancada: $INSTANCE_ID — aguardando estado 'running'..."

aws ec2 wait instance-running --profile "$AWS_PROFILE" --region "$AWS_REGION" --instance-ids "$INSTANCE_ID"

PUBLIC_IP=$(aws ec2 describe-instances --profile "$AWS_PROFILE" --region "$AWS_REGION" \
  --instance-ids "$INSTANCE_ID" \
  --query 'Reservations[0].Instances[0].PublicIpAddress' --output text)
save_state PUBLIC_IP "$PUBLIC_IP"
ok "EC2 running — IP publico: $PUBLIC_IP"

# === 8. Build + ship images + start app =================================
say "8/8 Build (linux/arm64) → save → scp → load → docker compose up"

# Build images localmente forcando arm64 (matching t4g.small)
ok "Buildando fiap-connect (arm64)..."
docker buildx build --platform linux/arm64 -t fiap-connect:local --load .. >/dev/null
ok "Buildando oracle-proxy (arm64)..."
docker buildx build --platform linux/arm64 -t oracle-proxy:local --load ../oracle-proxy >/dev/null

# Save images como tar.gz
ok "Exportando imagens..."
mkdir -p images
docker save fiap-connect:local | gzip > images/fiap-connect.tar.gz
docker save oracle-proxy:local | gzip > images/oracle-proxy.tar.gz

# Espera SSH ficar disponivel (porta 22 aberta + cloud-init terminou)
ok "Aguardando SSH ficar disponivel..."
for i in {1..60}; do
  if ssh -o ConnectTimeout=3 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null \
       -i "$KEY_PATH" ec2-user@"$PUBLIC_IP" "test -f /var/lib/cloud-init-done" 2>/dev/null; then
    ok "SSH + cloud-init prontos"
    break
  fi
  [ "$i" = "60" ] && { err "Timeout esperando SSH/cloud-init"; exit 1; }
  sleep 5
done

# Manda arquivos pro EC2
ok "Enviando arquivos pro EC2..."
SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -i $KEY_PATH"
ssh $SSH_OPTS ec2-user@"$PUBLIC_IP" "mkdir -p ~/app"
scp $SSH_OPTS \
  images/fiap-connect.tar.gz \
  images/oracle-proxy.tar.gz \
  docker-compose.yml \
  .env \
  "$HOME/secrets/serviceAccountKey.json" \
  ec2-user@"$PUBLIC_IP":~/app/

# Roda no EC2: load imagens + compose up
ok "Carregando imagens no Docker do EC2..."
ssh $SSH_OPTS ec2-user@"$PUBLIC_IP" bash -s <<'REMOTE'
set -euo pipefail
cd ~/app
docker load < fiap-connect.tar.gz
docker load < oracle-proxy.tar.gz
docker compose --env-file .env up -d
sleep 5
docker compose ps
REMOTE

ok "App subiu na AWS!"

# === Resumo final =======================================================
say "Deploy completo"
cat <<RESUMO

  🟢 STATUS: rodando em http://${PUBLIC_IP}:8080

  Testar:
    curl http://${PUBLIC_IP}:8080/health
    curl http://${PUBLIC_IP}:8080/api/Debug/ords
    open http://${PUBLIC_IP}:8080/swagger

  SSH:
    ssh -i ${KEY_PATH} ec2-user@${PUBLIC_IP}

  Logs da app remotamente:
    ssh -i ${KEY_PATH} ec2-user@${PUBLIC_IP} 'docker logs -f fiap-api'

  Parar EC2 (zera custo de compute, manteм disco):
    aws ec2 stop-instances --profile ${AWS_PROFILE} --region ${AWS_REGION} --instance-ids ${INSTANCE_ID}

  Religar:
    aws ec2 start-instances --profile ${AWS_PROFILE} --region ${AWS_REGION} --instance-ids ${INSTANCE_ID}
    (atencao: IP publico muda ao religar a menos que use Elastic IP)

  Destruir tudo (apaga VPC, EC2, SG, key pair):
    bash teardown.sh

  Estado salvo em: ${STATE_FILE}

RESUMO
