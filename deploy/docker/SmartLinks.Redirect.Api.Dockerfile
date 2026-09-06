# syntax=docker/dockerfile:1

ARG DOTNET_SDK_TAG=8.0.424-bookworm-slim
ARG DOTNET_ASPNET_TAG=8.0.30-bookworm-slim

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_TAG} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/SmartLinks.Contracts/SmartLinks.Contracts.csproj", "src/BuildingBlocks/SmartLinks.Contracts/"]
COPY ["src/BuildingBlocks/SmartLinks.RuleEngine/SmartLinks.RuleEngine.csproj", "src/BuildingBlocks/SmartLinks.RuleEngine/"]
COPY ["src/Redirect/SmartLinks.Redirect.Application/SmartLinks.Redirect.Application.csproj", "src/Redirect/SmartLinks.Redirect.Application/"]
COPY ["src/Redirect/SmartLinks.Redirect.Infrastructure/SmartLinks.Redirect.Infrastructure.csproj", "src/Redirect/SmartLinks.Redirect.Infrastructure/"]
COPY ["src/Redirect/SmartLinks.Redirect.Api/SmartLinks.Redirect.Api.csproj", "src/Redirect/SmartLinks.Redirect.Api/"]
RUN dotnet restore "src/Redirect/SmartLinks.Redirect.Api/SmartLinks.Redirect.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/Redirect/SmartLinks.Redirect.Api/SmartLinks.Redirect.Api.csproj" \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_ASPNET_TAG} AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

LABEL com.difingo.smartlinks.geoip.provider="DB-IP" \
      com.difingo.smartlinks.geoip.release="2026-09" \
      com.difingo.smartlinks.geoip.license="CC-BY-4.0" \
      com.difingo.smartlinks.geoip.source="https://db-ip.com/db/download/ip-to-country-lite"

RUN install -d --mode=0755 /var/lib/smartlinks \
    && install -d --mode=0755 /var/lib/smartlinks/geoip

COPY --chmod=0444 deploy/geoip/dbip-country-lite-2026-09.mmdb /var/lib/smartlinks/geoip/dbip-country-lite.mmdb

RUN printf '%s  %s\n' \
        "385d4ab1e08417634a0a64921ac0e9c15c4c5e8a" \
        "/var/lib/smartlinks/geoip/dbip-country-lite.mmdb" \
    | sha1sum --check -

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER ${APP_UID}
HEALTHCHECK --interval=5s --timeout=3s --start-period=5s --retries=12 \
    CMD ["curl", "--fail", "--silent", "--show-error", "--max-time", "2", "--output", "/dev/null", "http://127.0.0.1:8080/health/ready"]
ENTRYPOINT ["dotnet", "SmartLinks.Redirect.Api.dll"]