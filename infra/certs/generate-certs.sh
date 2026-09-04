#!/bin/sh
# Genera una CA local y un certificado para el CN "sqlserver", usados para
# el modo de conexión "endurecido" documentado en docs/security.md.
# Este script NO se ejecuta automáticamente: es una utilidad opcional para
# quien quiera activar SQL_TLS_MODE=strict.
#
# Uso: sh infra/certs/generate-certs.sh
set -e

CERT_DIR="$(dirname "$0")"
DAYS=825
CN="sqlserver"

cd "$CERT_DIR"

echo "Generando CA local..."
openssl req -x509 -nodes -newkey rsa:2048 -days "$DAYS" \
  -keyout ca.key -out ca.crt \
  -subj "/C=CO/O=FyM Technology/CN=FyM Local CA"

echo "Generando clave y CSR para ${CN}..."
openssl req -nodes -newkey rsa:2048 \
  -keyout mssql.key -out mssql.csr \
  -subj "/C=CO/O=FyM Technology/CN=${CN}"

echo "Firmando el certificado del servidor con la CA local..."
openssl x509 -req -in mssql.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
  -out mssql.crt -days "$DAYS" -sha256 \
  -extfile <(printf "subjectAltName=DNS:%s" "$CN")

# SQL Server exige que la clave privada no tenga passphrase y sea legible
# solo por su propio usuario dentro del contenedor.
chmod 600 mssql.key
rm -f mssql.csr ca.srl

echo "Listo. Archivos generados en ${CERT_DIR}: ca.crt, mssql.crt, mssql.key"
echo "Móntelos en el contenedor de SQL Server (ver infra/sqlserver/mssql.conf)"
echo "y active SQL_TLS_MODE=strict en su .env."
