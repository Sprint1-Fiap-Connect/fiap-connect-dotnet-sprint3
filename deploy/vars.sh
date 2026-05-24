#!/usr/bin/env bash
# Variaveis usadas pelos scripts de deploy. Source este arquivo antes de rodar:
#   source ./vars.sh

# Identificacao
export PROJECT="davi-lab"
export AWS_PROFILE="davi-pessoal"
export AWS_REGION="us-east-1"

# Network
export VPC_CIDR="10.20.0.0/16"
export SUBNET_CIDR="10.20.1.0/24"
export AZ="us-east-1a"

# EC2
# t4g.small = ARM Graviton, 2vCPU, 2GB RAM, Free Tier (750h/mes por 12 meses)
export INSTANCE_TYPE="t4g.small"
export INSTANCE_ARCH="arm64"
# Amazon Linux 2023 ARM64 via SSM parameter (sempre a AMI mais recente)
export AMI_SSM_PARAM="/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-arm64"

# Key pair
export KEY_NAME="${PROJECT}-key"
export KEY_PATH="$HOME/secrets/${KEY_NAME}.pem"

# Resource names (todos vao prefixados com $PROJECT)
export VPC_NAME="${PROJECT}-vpc"
export IGW_NAME="${PROJECT}-igw"
export SUBNET_NAME="${PROJECT}-public-${AZ}"
export RT_NAME="${PROJECT}-public-rt"
export SG_NAME="${PROJECT}-app-sg"
export INSTANCE_NAME="${PROJECT}-vm"
export EIP_NAME="${PROJECT}-eip"

# Tags comuns aplicados em todo recurso
export COMMON_TAGS="Key=Project,Value=${PROJECT} Key=Owner,Value=davi.praxedes Key=ManagedBy,Value=script"

# Arquivo de estado (guardamos IDs aqui pro teardown.sh reverter)
export STATE_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/.aws-state"
