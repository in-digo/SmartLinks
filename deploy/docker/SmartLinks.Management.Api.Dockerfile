# syntax=docker/dockerfile:1

ARG DOTNET_SDK_TAG=8.0.424-bookworm-slim
ARG DOTNET_ASPNET_TAG=8.0.30-bookworm-slim

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_TAG} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/SmartLinks.Contracts/SmartLinks.Contracts.csproj", "src/BuildingBlocks/SmartLinks.Contracts/"]
COPY ["src/BuildingBlocks/SmartLinks.RuleEngine/SmartLinks.RuleEngine.csproj", "src/BuildingBlocks/SmartLinks.RuleEngine/"]
COPY ["src/Management/SmartLinks.Management.Domain/SmartLinks.Management.Domain.csproj", "src/Management/SmartLinks.Management.Domain/"]
COPY ["src/Management/SmartLinks.Management.Application/SmartLinks.Management.Application.csproj", "src/Management/SmartLinks.Management.Application/"]
COPY ["src/Management/SmartLinks.Management.Infrastructure/SmartLinks.Management.Infrastructure.csproj", "src/Management/SmartLinks.Management.Infrastructure/"]
COPY ["src/Management/SmartLinks.Management.Api/SmartLinks.Management.Api.csproj", "src/Management/SmartLinks.Management.Api/"]
RUN dotnet restore "src/Management/SmartLinks.Management.Api/SmartLinks.Management.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/Management/SmartLinks.Management.Api/SmartLinks.Management.Api.csproj" \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migration-build
COPY [".config/dotnet-tools.json", ".config/"]
RUN dotnet tool restore
RUN dotnet ef migrations bundle \
    --project "src/Management/SmartLinks.Management.Infrastructure/SmartLinks.Management.Infrastructure.csproj" \
    --startup-project "src/Management/SmartLinks.Management.Infrastructure/SmartLinks.Management.Infrastructure.csproj" \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-build \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_ASPNET_TAG} AS runtime-base
WORKDIR /app

FROM runtime-base AS migrations
COPY --from=migration-build /app/efbundle .

USER ${APP_UID}
ENTRYPOINT ["./efbundle", "--no-color"]

FROM runtime-base AS api-runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM api-runtime AS final
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER ${APP_UID}
HEALTHCHECK --interval=5s --timeout=3s --start-period=5s --retries=12 \
    CMD ["curl", "--fail", "--silent", "--show-error", "--max-time", "2", "--output", "/dev/null", "http://127.0.0.1:8080/health/ready"]
ENTRYPOINT ["dotnet", "SmartLinks.Management.Api.dll"]