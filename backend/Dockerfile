# T-INFRA-10: Multi-stage Dockerfile for GOP 360° API (.NET 10)
# Stage 1: Build
# Stage 2: Publish (trimmed)
# Stage 3: Runtime (non-root, minimal)

# ── Stage 1: Restore & Build ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimization)
COPY GOP.slnx .
COPY src/GOP.Domain/GOP.Domain.csproj           src/GOP.Domain/
COPY src/GOP.Application/GOP.Application.csproj src/GOP.Application/
COPY src/GOP.Infrastructure/GOP.Infrastructure.csproj src/GOP.Infrastructure/
COPY src/GOP.API/GOP.API.csproj                 src/GOP.API/
COPY tests/GOP.Domain.Tests/GOP.Domain.Tests.csproj         tests/GOP.Domain.Tests/
COPY tests/GOP.Application.Tests/GOP.Application.Tests.csproj tests/GOP.Application.Tests/
COPY tests/GOP.Infrastructure.Tests/GOP.Infrastructure.Tests.csproj tests/GOP.Infrastructure.Tests/
COPY tests/GOP.API.Tests/GOP.API.Tests.csproj   tests/GOP.API.Tests/
COPY Directory.Build.props .

# Restore (layer cached unless .csproj files change)
RUN dotnet restore GOP.slnx

# Copy all sources
COPY . .

# Build Release
RUN dotnet build src/GOP.API/GOP.API.csproj \
    --configuration Release \
    --no-restore

# ── Stage 2: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish src/GOP.API/GOP.API.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish \
    /p:UseAppHost=false

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Security: non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser

WORKDIR /app

# Copy published output
COPY --from=publish --chown=appuser:appgroup /app/publish .

# App Service uses port 8080 by default for Linux containers
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Staging
EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "GOP.API.dll"]
