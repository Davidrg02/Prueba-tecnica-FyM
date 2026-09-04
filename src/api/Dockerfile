# syntax=docker/dockerfile:1

# ---- Restore & build ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero: esta capa se reutiliza mientras no
# cambien las dependencias, aunque el código fuente sí cambie.
COPY Directory.Build.props ./
COPY src/FyM.Users.Domain/FyM.Users.Domain.csproj src/FyM.Users.Domain/
COPY src/FyM.Users.Application/FyM.Users.Application.csproj src/FyM.Users.Application/
COPY src/FyM.Users.Infrastructure/FyM.Users.Infrastructure.csproj src/FyM.Users.Infrastructure/
COPY src/FyM.Users.Api/FyM.Users.Api.csproj src/FyM.Users.Api/
RUN dotnet restore src/FyM.Users.Api/FyM.Users.Api.csproj

COPY src/ src/
RUN dotnet publish src/FyM.Users.Api/FyM.Users.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

# El usuario "app" ya viene preconfigurado (UID 64198) en la imagen base de ASP.NET Core.
USER app

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=6 \
    CMD curl -f http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "FyM.Users.Api.dll"]
