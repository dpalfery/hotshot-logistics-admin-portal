# Docker Usage Guide for DbSetup Tool

## Overview

The HotshotLogistics DbSetup tool now supports Docker-based SQL Server provisioning with automatic container management and database migrations.

## Prerequisites

1. **Docker Desktop** must be installed and running
2. **.NET 8 SDK** installed
3. **docker-compose** CLI available (included with Docker Desktop)

## Quick Start with Docker

### 1. Basic Docker Setup

```bash
# Navigate to the project directory
cd 7-Deployment/DbSetup/HotshotLogistics.DbSetup

# Run with Docker mode
dotnet run --use-docker --sa-password "YourStrong@Passw0rd123"
```

This will:
- ✅ Check if Docker is running
- ✅ Start the SQL Server container using docker-compose
- ✅ Wait for SQL Server to be ready
- ✅ Run preflight checks
- ✅ Create the database
- ✅ Create application login and user
- ✅ Grant necessary permissions
- ✅ Run FluentMigrator migrations

### 2. Specify Database Name

```bash
dotnet run --use-docker \
  --sa-password "YourStrong@Passw0rd123" \
  --db-name "my_custom_db"
```

### 3. Custom docker-compose File Location

```bash
dotnet run --use-docker \
  --sa-password "YourStrong@Passw0rd123" \
  --docker-compose-file "C:\path\to\docker-compose.yml"
```

### 4. Non-Interactive Mode (CI/CD)

```bash
dotnet run --use-docker \
  --sa-password "YourStrong@Passw0rd123" \
  --non-interactive \
  --db-name "hotshot_logistics"
```

## Environment Variables

Instead of command-line arguments, you can use environment variables:

```bash
# Set environment variables
$env:HOTSHOT_DB_SA_PASSWORD = "YourStrong@Passw0rd123"
$env:HOTSHOT_DB_NAME = "hotshot_logistics"
$env:HOTSHOT_DB_APP_USER = "hotshot_app"

# Run with Docker
dotnet run --use-docker
```

Available environment variables:
- `HOTSHOT_DB_SA_PASSWORD` - SA password for SQL Server
- `HOTSHOT_DB_NAME` - Target database name
- `HOTSHOT_DB_APP_USER` - Application user name
- `HOTSHOT_DB_PASSWORD` - Application user password (auto-generated if not provided)
- `HOTSHOT_DOCKER_COMPOSE_FILE` - Path to docker-compose.yml

## Docker Compose Configuration

The tool uses the docker-compose.yml file located at `7-Deployment/docker-compose.yml`:

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: hotshot_sqlserver
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${SQL_SA_PASSWORD}
      MSSQL_PID: "Developer"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    restart: unless-stopped

volumes:
  sqlserver_data:
    name: hotshot_sqlserver_data
```

## Features

### 1. Automatic Container Management
- Detects if Docker is running
- Checks if container is already running (reuses existing container)
- Starts container if not running
- Waits for SQL Server to be ready (with retry logic)

### 2. Preflight Checks
- SQL Server connectivity
- Privileged credentials validation
- Create database permissions
- Create login permissions
- Database access verification

### 3. Database Provisioning
- Creates database if it doesn't exist
- Creates SQL Server login
- Creates database user
- Grants appropriate permissions (CONNECT, SELECT, INSERT, UPDATE, DELETE, EXECUTE on dbo schema)

### 4. Migration Execution
- Automatically runs FluentMigrator migrations from `4-Persistence/HotshotLogistics.Data`
- Supports both in-process and subprocess execution
- Provides detailed logging of migration progress

### 5. Rollback on Failure
- Automatic cleanup if provisioning fails
- Removes created artifacts in reverse order

## Connection String Output

After successful setup, the tool outputs the application connection string:

```
Connection string generated:
Server=localhost,1433;Database=hotshot_logistics;User Id=hotshot_app;Password=<generated-password>;TrustServerCertificate=true;

You can set this as an environment variable:
  DB_CONNECTION_STRING=Server=localhost,1433;Database=hotshot_logistics;User Id=hotshot_app;Password=<generated-password>;TrustServerCertificate=true;
```

## Troubleshooting

### Docker Not Running
```
Error: Docker is not running. Please start Docker Desktop and try again.
```
**Solution:** Start Docker Desktop and ensure it's fully initialized.

### SQL Server Not Ready
```
Error: SQL Server failed to become ready in time
```
**Solution:** The container may need more time to start. Check Docker logs:
```bash
docker logs hotshot_sqlserver
```

### Container Already Exists
If the container is already running, the tool will detect and use it automatically.

### Permissions Issues
```
Error: Preflight checks failed
```
**Solution:** Ensure the SA password is correct and has sufficient privileges.

## Manual Container Management

### Start Container Manually
```bash
cd 7-Deployment
docker-compose up -d
```

### Stop Container
```bash
cd 7-Deployment
docker-compose down
```

### Remove Container and Data
```bash
cd 7-Deployment
docker-compose down -v
```

### View Logs
```bash
docker logs hotshot_sqlserver
```

### Connect to Container
```bash
docker exec -it hotshot_sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd123"
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Setup Database
  run: |
    dotnet run --project 7-Deployment/DbSetup/HotshotLogistics.DbSetup/HotshotLogistics.DbSetup.csproj `
      --use-docker `
      --sa-password "${{ secrets.SQL_SA_PASSWORD }}" `
      --non-interactive
```

### Azure DevOps Example

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Setup Database'
  inputs:
    command: 'run'
    projects: '7-Deployment/DbSetup/HotshotLogistics.DbSetup/HotshotLogistics.DbSetup.csproj'
    arguments: '--use-docker --sa-password "$(SQL_SA_PASSWORD)" --non-interactive'
```

## Command-Line Options Summary

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--use-docker` | Enable Docker mode | No | false |
| `--sa-password` | SA password for SQL Server | Yes (with Docker) | - |
| `--docker-compose-file` | Path to docker-compose.yml | No | `7-Deployment/docker-compose.yml` |
| `--db-name` | Database name | No | `hotshot_logistics` |
| `--app-user` | Application user name | No | `hotshot_app` |
| `--password` | Application user password | No | Auto-generated |
| `--non-interactive` | Run without prompts | No | false |
| `--server` | SQL Server instance | No | `localhost` (Docker) |

## Security Notes

⚠️ **Important Security Considerations:**

1. **Never commit SA passwords** to source control
2. **Use environment variables** or secure secret management for passwords
3. **SA passwords must meet** SQL Server complexity requirements:
   - At least 8 characters
   - Contains uppercase letters
   - Contains lowercase letters
   - Contains digits
   - Contains special characters

4. **Application passwords** are auto-generated with cryptographic security (24 characters by default)

5. **Connection strings** contain passwords - handle them securely

## What's Next?

After running the setup:

1. Your database is ready at `localhost,1433`
2. All migrations have been applied
3. Use the generated connection string in your application
4. The container will persist data in the `hotshot_sqlserver_data` volume

## Additional Resources

- [Main README](README.md) - Full documentation
- [CLI Usage Guide](../../6-Docs/specs/dbsetup/cli-usage.md) - Detailed CLI examples
- [Docker Compose Reference](https://docs.docker.com/compose/)
- [SQL Server on Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
