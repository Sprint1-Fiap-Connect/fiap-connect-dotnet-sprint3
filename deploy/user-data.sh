#!/bin/bash
# Cloud-init: roda na primeira boot da EC2 como root
# Instala Docker + plugin Compose v2 (ARM64)
set -euxo pipefail

dnf update -y
dnf install -y docker

systemctl enable --now docker
usermod -aG docker ec2-user

# Docker Compose v2 plugin
DOCKER_CONFIG=/usr/local/lib/docker
mkdir -p $DOCKER_CONFIG/cli-plugins
curl -SL "https://github.com/docker/compose/releases/download/v2.27.1/docker-compose-linux-aarch64" \
  -o $DOCKER_CONFIG/cli-plugins/docker-compose
chmod +x $DOCKER_CONFIG/cli-plugins/docker-compose

# Sinaliza que terminou (deploy.sh espera esse arquivo aparecer)
touch /var/lib/cloud-init-done
