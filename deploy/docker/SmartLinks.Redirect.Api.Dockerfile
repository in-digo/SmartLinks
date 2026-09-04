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
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER ${APP_UID}
ENTRYPOINT ["dotnet", "SmartLinks.Redirect.Api.dll"]