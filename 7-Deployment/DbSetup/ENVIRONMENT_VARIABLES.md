# Environment Variables Guide

## Overview

The DbSetup tool uses a **project slug-based** naming convention for environment variables, making them consistent, clean, and project-specific.

## Project Slug

The `--project-slug` parameter (default: `"app"`) determines the prefix for all environment variables.

**Format**: `{PROJECT_SLUG}_DB_*`

### Examples

```bash
# Project slug: "hotshot"
HOTSHOT_DB_SERVER=localhost
HOTSHOT_DB_PORT=1433
HOTSHOT_DB_NAME=logistics_db
HOTSHOT_DB_APP_USER=hotshot_app
HOTSHOT_DB_APP_PASSWORD=YourAppPassword123!
HOTSHOT_DB_SA_PASSWORD=YourSA@Pass123
HOTSHOT_DB_USE_DOCKER=true
HOTSHOT_DB_DOCKER_COMPOSE_FILE=C:\path\to\docker-compose.yml

# Project slug: "myapp"
MYAPP_DB_SERVER=localhost
MYAPP_DB_PORT=1433
MYAPP_DB_NAME=myapp_db
```

## Available Environment Variables

| Variable Pattern | Description | Example Value | Required |
|-----------------|-------------|---------------|----------|
| `{SLUG}_DB_SERVER` | SQL Server instance | `localhost` or `localhost\SQLEXPRESS` | Interactive prompt if missing |
| `{SLUG}_DB_PORT` | SQL Server port | `1433` | No (default: 1433) |
| `{SLUG}_DB_NAME` | Database name | `hotshot_logistics` | Interactive prompt if missing |
| `{SLUG}_DB_APP_USER` | Application user name | `hotshot_app` | Interactive prompt if missing |
| `{SLUG}_DB_APP_PASSWORD` | Application user password | Auto-generated | No (auto-generated) |
| `{SLUG}_DB_SA_PASSWORD` | SA password | `YourSA@Pass123` | Yes (if using Docker) |
| `{SLUG}_DB_USE_DOCKER` | Use Docker mode | `true` or `false` | No (default: false) |
| `{SLUG}_DB_DOCKER_COMPOSE_FILE` | Path to docker-compose.yml | `C:\path\to\docker-compose.yml` | No (auto-detected) |

## Usage Modes

### 1. Command-Line Parameters (Highest Priority)

```bash
dotnet run --project-slug "hotshot" \
  --use-docker \
  --sa-password "YourSA@Pass123" \
  --server "localhost" \
  --db-name "logistics_db"
```

### 2. Environment Variables (Medium Priority)

```powershell
# Set environment variables
$env:HOTSHOT_DB_SERVER = "localhost"
$env:HOTSHOT_DB_PORT = "1433"
$env:HOTSHOT_DB_NAME = "logistics_db"
$env:HOTSHOT_DB_APP_USER = "hotshot_app"
$env:HOTSHOT_DB_SA_PASSWORD = "YourSA@Pass123"
$env:HOTSHOT_DB_USE_DOCKER = "true"

# Run with minimal parameters
dotnet run --project-slug "hotshot"
```

### 3. Interactive Prompts (Lowest Priority)

If neither command-line parameters nor environment variables are provided (and not in `--non-interactive` mode), the tool will prompt for missing values:

```bash
dotnet run --project-slug "hotshot" --use-docker
```

Output:
```
Enter SA password for SQL Server (min 8 chars, mixed case, numbers, symbols): ****
SQL Server instance (default: localhost): [Enter]
Database name (default: hotshot_db): [Enter]
Application user name (default: hotshot_app): [Enter]
```

## Priority Order

1. **Command-line arguments** (highest)
2. **Environment variables** 
3. **Interactive prompts** (if not `--non-interactive`)
4. **Defaults** (lowest)

## Setting Environment Variables

### PowerShell (Windows)

```powershell
# Temporary (current session only)
$env:HOTSHOT_DB_SERVER = "localhost"
$env:HOTSHOT_DB_PORT = "1433"
$env:HOTSHOT_DB_NAME = "logistics_db"
$env:HOTSHOT_DB_SA_PASSWORD = "YourSA@Pass123"
$env:HOTSHOT_DB_USE_DOCKER = "true"

# Persistent (user level)
[System.Environment]::SetEnvironmentVariable('HOTSHOT_DB_SERVER', 'localhost', 'User')
[System.Environment]::SetEnvironmentVariable('HOTSHOT_DB_PORT', '1433', 'User')
[System.Environment]::SetEnvironmentVariable('HOTSHOT_DB_NAME', 'logistics_db', 'User')
[System.Environment]::SetEnvironmentVariable('HOTSHOT_DB_SA_PASSWORD', 'YourSA@Pass123', 'User')
[System.Environment]::SetEnvironmentVariable('HOTSHOT_DB_USE_DOCKER', 'true', 'User')
```

### Bash (Linux/macOS)

```bash
# Temporary (current session only)
export HOTSHOT_DB_SERVER="localhost"
export HOTSHOT_DB_PORT="1433"
export HOTSHOT_DB_NAME="logistics_db"
export HOTSHOT_DB_SA_PASSWORD="YourSA@Pass123"
export HOTSHOT_DB_USE_DOCKER="true"

# Persistent (add to ~/.bashrc or ~/.bash_profile)
echo 'export HOTSHOT_DB_SERVER="localhost"' >> ~/.bashrc
echo 'export HOTSHOT_DB_PORT="1433"' >> ~/.bashrc
echo 'export HOTSHOT_DB_NAME="logistics_db"' >> ~/.bashrc
echo 'export HOTSHOT_DB_SA_PASSWORD="YourSA@Pass123"' >> ~/.bashrc
echo 'export HOTSHOT_DB_USE_DOCKER="true"' >> ~/.bashrc
source ~/.bashrc
```

## Non-Interactive Mode (CI/CD)

In non-interactive mode, all required values must be provided via command-line parameters or environment variables:

```bash
dotnet run --project-slug "hotshot" \
  --use-docker \
  --sa-password "${{ secrets.SQL_SA_PASSWORD }}" \
  --non-interactive
```

Or with environment variables:

```yaml
# GitHub Actions example
env:
  HOTSHOT_DB_SERVER: localhost
  HOTSHOT_DB_PORT: 1433
  HOTSHOT_DB_NAME: logistics_db
  HOTSHOT_DB_APP_USER: hotshot_app
  HOTSHOT_DB_SA_PASSWORD: ${{ secrets.SQL_SA_PASSWORD }}
  HOTSHOT_DB_USE_DOCKER: true

run: dotnet run --project-slug "hotshot" --non-interactive
```

## Best Practices

### 1. Use Project-Specific Slugs

```bash
# Good - project-specific
dotnet run --project-slug "hotshot"      # Creates HOTSHOT_DB_* vars
dotnet run --project-slug "inventory"    # Creates INVENTORY_DB_* vars

# Avoid - generic
dotnet run --project-slug "app"          # Too generic
```

### 2. Keep Secrets Secure

**Never commit passwords to source control**

```powershell
# ✅ Good - use secrets management
$env:HOTSHOT_DB_SA_PASSWORD = (Get-Secret -Name "sql-sa-password")

# ❌ Bad - hardcoded password
$env:HOTSHOT_DB_SA_PASSWORD = "MyPassword123!"
```

### 3. Document Your Project's Variables

Create a `.env.example` file (without actual secrets):

```bash
# .env.example
HOTSHOT_DB_SERVER=localhost
HOTSHOT_DB_PORT=1433
HOTSHOT_DB_NAME=logistics_db
HOTSHOT_DB_APP_USER=hotshot_app
HOTSHOT_DB_APP_PASSWORD=<auto-generated>
HOTSHOT_DB_SA_PASSWORD=<your-sa-password>
HOTSHOT_DB_USE_DOCKER=true
```

### 4. Use Defaults Wisely

The tool provides smart defaults based on the project slug:

- Database name: `{project_slug}_db`
- App user: `{project_slug}_app`
- Server: `localhost` (Docker) or `localhost\SQLEXPRESS` (non-Docker)
- Port: `1433`

## Migration from Old Environment Variables

### Old (Inconsistent)

```bash
HOTSHOT_DB_SERVER=localhost
HOTSHOT_DB_NAME=hotshot_logistics
HOTSHOT_DB_APP_USER=hotshot_app
HOTSHOT_DB_PASSWORD=confusing             # Which password?
CI_SA_CONNECTION_STRING=Server=...        # Inconsistent naming
HOTSHOT_DOCKER_COMPOSE_FILE=path
```

### New (Consistent)

```bash
HOTSHOT_DB_SERVER=localhost
HOTSHOT_DB_PORT=1433                      # New: explicit port
HOTSHOT_DB_NAME=hotshot_logistics
HOTSHOT_DB_APP_USER=hotshot_app
HOTSHOT_DB_APP_PASSWORD=app_password      # Clear: app password
HOTSHOT_DB_SA_PASSWORD=sa_password        # Clear: SA password
HOTSHOT_DB_USE_DOCKER=true                # New: explicit flag
HOTSHOT_DB_DOCKER_COMPOSE_FILE=path       # Consistent prefix
```

## Troubleshooting

### Variables Not Being Picked Up

1. **Check project slug**: Ensure you're using the correct project slug
   ```bash
   # If you set MYAPP_DB_*, must use --project-slug "myapp"
   dotnet run --project-slug "myapp"
   ```

2. **Verify environment variable names**: They must match exactly (case-sensitive on Linux/macOS)

3. **Restart your shell**: Some changes require a new shell session

### Testing Your Environment

```bash
# Test that variables are set
dotnet run --project-slug "hotshot" --help
# Should show your project slug and available options

# Dry run to verify settings
dotnet run --project-slug "hotshot" --non-interactive
# Will fail fast if required variables are missing
```

## Examples by Scenario

### Scenario 1: First Time Setup (Interactive)

```bash
cd 7-Deployment/DbSetup/HotshotLogistics.DbSetup
dotnet run --project-slug "hotshot" --use-docker
# Tool will prompt for SA password, then use defaults for everything else
```

### Scenario 2: Automated Setup (CI/CD)

```bash
export HOTSHOT_DB_SA_PASSWORD="$SECRET_SA_PASSWORD"
export HOTSHOT_DB_USE_DOCKER="true"
dotnet run --project-slug "hotshot" --non-interactive
```

### Scenario 3: Custom Configuration

```bash
dotnet run --project-slug "mycompany" \
  --server "sqlserver.mycompany.com" \
  --port 1433 \
  --db-name "production_db" \
  --app-user "prod_app" \
  --sa-password "SecurePass123!"
```

### Scenario 4: Multiple Projects

```bash
# Project 1: Hotshot Logistics
export HOTSHOT_DB_NAME="logistics_db"
export HOTSHOT_DB_SA_PASSWORD="pass1"
dotnet run --project-slug "hotshot" --use-docker

# Project 2: Inventory System
export INVENTORY_DB_NAME="inventory_db"
export INVENTORY_DB_SA_PASSWORD="pass2"
dotnet run --project-slug "inventory" --use-docker
```

## See Also

- [Docker Usage Guide](DOCKER_USAGE.md) - Docker-specific setup
- [Main README](README.md) - Complete documentation
- [CLI Usage Guide](../../6-Docs/specs/dbsetup/cli-usage.md) - Detailed examples
