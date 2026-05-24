#!/usr/bin/env bash
# Destroi todos os recursos AWS criados pelo deploy.sh.
# Le os IDs salvos em .aws-state e remove em ordem reversa.
#
# Uso: bash teardown.sh

set -uo pipefail
cd "$(dirname "$0")"
source ./vars.sh

say() { echo -e "\n\033[1;36m▶ $*\033[0m"; }
ok()  { echo -e "  \033[1;32m✓\033[0m $*"; }
warn(){ echo -e "  \033[1;33m!\033[0m $*"; }

if [ ! -f "$STATE_FILE" ]; then
  warn "Nenhum estado encontrado em $STATE_FILE - nada pra destruir"
  exit 0
fi

# Carrega IDs do state file
# shellcheck disable=SC1090
source "$STATE_FILE"

echo ""
echo "Vai destruir (na conta $AWS_PROFILE / $AWS_REGION):"
grep -v '^$' "$STATE_FILE" | sed 's/^/  - /'
echo ""
read -r -p "Confirma destruir TUDO acima? (digite 'yes' pra confirmar): " ans
[ "$ans" = "yes" ] || { warn "Abortado"; exit 0; }

# Helper que ignora erro se o recurso ja foi removido
try() { "$@" 2>/dev/null || true; }

if [ "${EIP_ASSOC_ID:-}" ]; then
  say "Desassociando Elastic IP"
  try aws ec2 disassociate-address --profile "$AWS_PROFILE" --region "$AWS_REGION" --association-id "$EIP_ASSOC_ID"
  ok "EIP desassociado"
fi

if [ "${EIP_ALLOC_ID:-}" ]; then
  say "Liberando Elastic IP $EIP_ADDR"
  try aws ec2 release-address --profile "$AWS_PROFILE" --region "$AWS_REGION" --allocation-id "$EIP_ALLOC_ID"
  ok "EIP liberado (evita cobranca de IP nao usado)"
fi

if [ "${INSTANCE_ID:-}" ]; then
  say "Terminando EC2 $INSTANCE_ID"
  try aws ec2 terminate-instances --profile "$AWS_PROFILE" --region "$AWS_REGION" --instance-ids "$INSTANCE_ID" >/dev/null
  ok "Aguardando termino completo..."
  try aws ec2 wait instance-terminated --profile "$AWS_PROFILE" --region "$AWS_REGION" --instance-ids "$INSTANCE_ID"
  ok "EC2 terminada"
fi

if [ "${KEY_NAME:-}" ]; then
  say "Removendo key pair $KEY_NAME"
  try aws ec2 delete-key-pair --profile "$AWS_PROFILE" --region "$AWS_REGION" --key-name "$KEY_NAME"
  rm -f "$KEY_PATH"
  ok "Key pair removida (AWS + local)"
fi

if [ "${SG_ID:-}" ]; then
  say "Removendo security group $SG_ID"
  try aws ec2 delete-security-group --profile "$AWS_PROFILE" --region "$AWS_REGION" --group-id "$SG_ID"
  ok "SG removido"
fi

if [ "${RT_ASSOC:-}" ]; then
  say "Desassociando route table"
  try aws ec2 disassociate-route-table --profile "$AWS_PROFILE" --region "$AWS_REGION" --association-id "$RT_ASSOC"
fi

if [ "${RT_ID:-}" ]; then
  say "Removendo route table $RT_ID"
  try aws ec2 delete-route-table --profile "$AWS_PROFILE" --region "$AWS_REGION" --route-table-id "$RT_ID"
fi

if [ "${SUBNET_ID:-}" ]; then
  say "Removendo subnet $SUBNET_ID"
  try aws ec2 delete-subnet --profile "$AWS_PROFILE" --region "$AWS_REGION" --subnet-id "$SUBNET_ID"
fi

if [ "${IGW_ID:-}" ] && [ "${VPC_ID:-}" ]; then
  say "Desatachando + removendo IGW $IGW_ID"
  try aws ec2 detach-internet-gateway --profile "$AWS_PROFILE" --region "$AWS_REGION" --internet-gateway-id "$IGW_ID" --vpc-id "$VPC_ID"
  try aws ec2 delete-internet-gateway --profile "$AWS_PROFILE" --region "$AWS_REGION" --internet-gateway-id "$IGW_ID"
fi

if [ "${VPC_ID:-}" ]; then
  say "Removendo VPC $VPC_ID"
  try aws ec2 delete-vpc --profile "$AWS_PROFILE" --region "$AWS_REGION" --vpc-id "$VPC_ID"
fi

rm -f "$STATE_FILE"

say "Teardown completo"
ok "Verifique no console AWS se nao sobrou nada: https://console.aws.amazon.com/ec2/home?region=$AWS_REGION"
