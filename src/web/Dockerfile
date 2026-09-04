# syntax=docker/dockerfile:1
# NOTA: el contexto de build de este Dockerfile es la RAÍZ del repositorio
# (ver docker-compose.yml: build.context: "."), para poder copiar
# infra/nginx/ además del código de src/web/.

# ---- Build de Angular ----------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

COPY src/web/package*.json ./
RUN npm ci

COPY src/web/ .
RUN npm run build -- --configuration production

# ---- Runtime: Nginx sirviendo el bundle estático --------------------------
FROM nginx:alpine AS runtime

COPY --from=build /app/dist/web/browser /usr/share/nginx/html
COPY infra/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY infra/nginx/docker-entrypoint.sh /docker-entrypoint.d/40-fym-config.sh
# Normaliza a LF por si el script llegó con CRLF (Windows + core.autocrlf al
# clonar el repositorio): un shebang "#!/bin/sh\r" hace que el intérprete no
# se encuentre dentro del contenedor Linux.
RUN tr -d '\r' < /docker-entrypoint.d/40-fym-config.sh > /tmp/40-fym-config.sh && \
    mv /tmp/40-fym-config.sh /docker-entrypoint.d/40-fym-config.sh && \
    chmod +x /docker-entrypoint.d/40-fym-config.sh

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=6 \
    CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
