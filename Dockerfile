# ── Build stage ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (layer caching for NuGet restore)
COPY VTOS.Domain/VTOS.Domain.csproj VTOS.Domain/
COPY VTOS.Shared/VTOS.Shared.csproj VTOS.Shared/
COPY VTOS.Application/VTOS.Application.csproj VTOS.Application/
COPY VTOS.Infrastructure/VTOS.Infrastructure.csproj VTOS.Infrastructure/
COPY VTOS.API/VTOS.API.csproj VTOS.API/

RUN dotnet restore VTOS.API/VTOS.API.csproj

# Copy everything and build
COPY . .
RUN dotnet publish VTOS.API/VTOS.API.csproj -c Release -o /app/publish --no-restore

# ── Runtime stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install ICU for globalization support (Vietnamese text)
RUN apt-get update && apt-get install -y --no-install-recommends libicu-dev && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# .NET 8 defaults to port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "VTOS.API.dll"]
