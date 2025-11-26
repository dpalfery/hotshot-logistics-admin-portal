# =============================================================================
# STAGE 1: Build the application
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY ["HotshotLogistics.sln", "./"]
COPY ["0-Base/HotshotLogistics.Core/HotshotLogistics.Core.csproj", "0-Base/HotshotLogistics.Core/"]
COPY ["1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj", "1-Presentation/HotshotLogistics.Api/"]
COPY ["2-Application/HotshotLogistics.Application/HotshotLogistics.Application.csproj", "2-Application/HotshotLogistics.Application/"]
COPY ["3-Domain/HotshotLogistics.Contracts/HotshotLogistics.Contracts.csproj", "3-Domain/HotshotLogistics.Contracts/"]
COPY ["3-Domain/HotshotLogistics.Domain/HotshotLogistics.Domain.csproj", "3-Domain/HotshotLogistics.Domain/"]
COPY ["4-Persistence/HotshotLogistics.Data/HotshotLogistics.Data.csproj", "4-Persistence/HotshotLogistics.Data/"]

# Restore dependencies (cached unless .csproj files change)
RUN dotnet restore "1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj"

# Copy source code
COPY . .

# Build and publish in Release mode
WORKDIR "/src/1-Presentation/HotshotLogistics.Api"
RUN dotnet publish "HotshotLogistics.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =============================================================================
# STAGE 2: Production runtime (Chiseled Ubuntu - Ultra-minimal & Secure)
# =============================================================================
# Using chiseled-extra which includes ICU libraries for globalization support
# Benefits of chiseled images:
# - ~100MB smaller than standard images
# - No shell, no package manager (reduced attack surface)
# - Non-root by default
# - No apt/dpkg vulnerabilities
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra AS runtime
WORKDIR /app

# Copy published files (chiseled images run as non-root 'app' user by default)
COPY --from=build /app/publish .

# Expose port (Azure Container Apps uses 8080 by default)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# NOTE: Health checks in chiseled images must use ASP.NET Core's built-in health check middleware
# since curl/wget are not available. Configure health checks in Azure Container Apps instead.
# The app should expose a /health endpoint that returns 200 OK.

# Start the application
ENTRYPOINT ["dotnet", "HotshotLogistics.Api.dll"]
