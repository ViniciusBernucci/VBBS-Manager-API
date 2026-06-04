# ─── Stage 1: restore + build ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so restore layer is cached independently
COPY VBBSManager.sln .
COPY src/VBBSManager.Api/VBBSManager.Api.csproj             src/VBBSManager.Api/
COPY src/VBBSManager.Domain/VBBSManager.Domain.csproj       src/VBBSManager.Domain/
COPY src/VBBSManager.Infrastructure/VBBSManager.Infrastructure.csproj src/VBBSManager.Infrastructure/
COPY tests/VBBSManager.Tests/VBBSManager.Tests.csproj       tests/VBBSManager.Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish src/VBBSManager.Api/VBBSManager.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Stage 2: runtime image ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p logs

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "VBBSManager.Api.dll"]
