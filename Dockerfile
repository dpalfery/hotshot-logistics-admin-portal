# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["HotshotLogistics.sln", "./"]
COPY ["0-Base/HotshotLogistics.Core/HotshotLogistics.Core.csproj", "0-Base/HotshotLogistics.Core/"]
COPY ["1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj", "1-Presentation/HotshotLogistics.Api/"]
COPY ["2-Application/HotshotLogistics.Application/HotshotLogistics.Application.csproj", "2-Application/HotshotLogistics.Application/"]
COPY ["3-Domain/HotshotLogistics.Contracts/HotshotLogistics.Contracts.csproj", "3-Domain/HotshotLogistics.Contracts/"]
COPY ["3-Domain/HotshotLogistics.Domain/HotshotLogistics.Domain.csproj", "3-Domain/HotshotLogistics.Domain/"]
COPY ["4-Persistence/HotshotLogistics.Data/HotshotLogistics.Data.csproj", "4-Persistence/HotshotLogistics.Data/"]

# Restore dependencies
RUN dotnet restore "1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/1-Presentation/HotshotLogistics.Api"
RUN dotnet build "HotshotLogistics.Api.csproj" -c Release -o /app/build
RUN dotnet publish "HotshotLogistics.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "HotshotLogistics.Api.dll"]
