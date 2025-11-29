# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project files
# Note: We need to copy the Data project as well since it's referenced
COPY ["4-Persistence/HotshotLogistics.Data/HotshotLogistics.Data.csproj", "4-Persistence/HotshotLogistics.Data/"]
COPY ["3-Domain/HotshotLogistics.Domain/HotshotLogistics.Domain.csproj", "3-Domain/HotshotLogistics.Domain/"]
COPY ["3-Domain/HotshotLogistics.Contracts/HotshotLogistics.Contracts.csproj", "3-Domain/HotshotLogistics.Contracts/"]
COPY ["7-Deployment/HotshotLogistics.DbMigrationJob/HotshotLogistics.DbMigrationJob.csproj", "7-Deployment/HotshotLogistics.DbMigrationJob/"]

# Restore dependencies
RUN dotnet restore "7-Deployment/HotshotLogistics.DbMigrationJob/HotshotLogistics.DbMigrationJob.csproj"

# Copy the rest of the source code
COPY . .

# Build the project
WORKDIR "/app/7-Deployment/HotshotLogistics.DbMigrationJob"
RUN dotnet build "HotshotLogistics.DbMigrationJob.csproj" -c Release -o /app/build

# Publish the project
FROM build AS publish
RUN dotnet publish "HotshotLogistics.DbMigrationJob.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "HotshotLogistics.DbMigrationJob.dll"]
