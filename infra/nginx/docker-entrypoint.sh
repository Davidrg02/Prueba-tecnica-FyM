#!/bin/sh
# Se ejecuta automáticamente por el entrypoint oficial de la imagen nginx
# (convención /docker-entrypoint.d/*.sh) antes de arrancar el servidor.
# Reescribe config.json con la URL de la API en tiempo de arranque del
# contenedor, para que la misma imagen sirva en cualquier entorno.
set -e

CONFIG_PATH="/usr/share/nginx/html/config.json"
API_BASE_URL="${API_BASE_URL:-/api}"

cat > "$CONFIG_PATH" <<EOF
{
  "apiBaseUrl": "${API_BASE_URL}"
}
EOF

echo "[fym-web] config.json escrito con apiBaseUrl=${API_BASE_URL}"
