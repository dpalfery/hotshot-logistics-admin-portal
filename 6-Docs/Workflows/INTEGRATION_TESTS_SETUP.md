# Integration Tests Setup Guide

This guide explains how to configure GitHub Actions to run integration tests with SQL Server.

## Overview

The `integration-tests.yml` workflow automatically:
1. Spins up a SQL Server 2022 container
2. Waits for SQL Server to be ready
3. Runs the DbSetup CLI to provision and seed the database
4. Executes integration tests against the configured database
5. Uploads test results as artifacts

## Required GitHub Secrets

### `SQL_SA_PASSWORD`

**Purpose**: SQL Server SA (system administrator) password for the containerized database in CI/CD.

**Requirements**:
- Minimum 8 characters
- Must contain uppercase letters
- Must contain lowercase letters
- Must contain numbers
- Must contain special characters (e.g., `!`, `@`, `#`, `$`)

**Example**: `YourStrong!Passw0rd`

### How to Set Up GitHub Secrets

#### Method 1: GitHub Web Interface

1. Go to your repository on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `SQL_SA_PASSWORD`
5. Value: Enter a strong password (e.g., `MySecure!Pass123`)
6. Click **Add secret**

#### Method 2: GitHub CLI

```bash
# Set the SQL_SA_PASSWORD secret
gh secret set SQL_SA_PASSWORD --body "YourStrong!Passw0rd"

# Verify it was set (value will be hidden)
gh secret list
```

#### Method 3: Using a Password Generator

```bash
# Generate a strong password
STRONG_PASSWORD=$(openssl rand -base64 32 | tr -dc 'A-Za-z0-9!@#$%' | head -c 24)
echo "Generated password: $STRONG_PASSWORD"

# Set the secret
gh secret set SQL_SA_PASSWORD --body "$STRONG_PASSWORD"
```

## Workflow Architecture

### SQL Server Service Container

The workflow uses GitHub Actions service containers to run SQL Server:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    env:
      ACCEPT_EULA: Y
      SA_PASSWORD: ${{ secrets.SQL_SA_PASSWORD }}
      MSSQL_PID: Developer
    ports:
      - 1433:1433
```

**Key Features**:
- **Isolated**: Each workflow run gets a fresh database
- **Fast**: Container starts in ~30-60 seconds
- **Consistent**: Same SQL Server version as production
- **No Cost**: Uses GitHub's infrastructure
- **Health Checks**: Automatically verifies SQL Server is ready

### Database Provisioning with DbSetup CLI

The workflow uses your existing DbSetup CLI tool to configure the database:

```bash
dotnet run --project-slug "hotshot" \
  --non-interactive \
  --server "localhost" \
  --db-name "hotshot_logistics" \
  --app-user "hotshot_app" \
  --password "${{ secrets.SQL_SA_PASSWORD }}" \
  --sa-password "${{ secrets.SQL_SA_PASSWORD }}"
```

**What DbSetup Does**:
1. Creates the database (`hotshot_logistics`)
2. Creates the application user (`hotshot_app`)
3. Grants necessary permissions
4. Runs FluentMigrator migrations
5. Seeds test data

### Integration Tests

Tests run with the `DB_CONNECTION_STRING` environment variable:

```yaml
env:
  DB_CONNECTION_STRING: "Server=localhost,1433;Database=hotshot_logistics;User Id=sa;Password=${{ secrets.SQL_SA_PASSWORD }};TrustServerCertificate=true;"
```

**Why SA credentials for tests?**
- Simplifies CI/CD configuration (only one secret needed)
- Tests run in an isolated container (no security risk)
- Consistent with local development practices
- Full permissions for test database operations

## Security Best Practices

### ✅ DO

- **Use GitHub Secrets** for all passwords and connection strings
- **Generate strong passwords** with mixed character types
- **Rotate secrets regularly** (every 90 days recommended)
- **Use different passwords** for different environments (dev, staging, prod)
- **Limit secret access** to necessary workflows only
- **Use TrustServerCertificate=true** for local/CI connections (container doesn't have trusted certs)

### ❌ DON'T

- **Never hardcode passwords** in workflow files
- **Never commit secrets** to source control
- **Never log secrets** in workflow output
- **Don't use production passwords** for CI/CD
- **Don't share secrets** across unrelated repositories
- **Don't use weak passwords** (e.g., `Password123`)

## Workflow Triggers

The workflow runs automatically on:

1. **Pull Requests** that modify:
   - C# files (`**/*.cs`)
   - Project files (`**/*.csproj`)
   - Persistence layer (`4-Persistence/**`)
   - Tests (`5-Test/**`)
   - The workflow itself

2. **Pushes to main/develop** that modify the same paths

3. **Manual trigger** via `workflow_dispatch`

## Troubleshooting

### SQL Server Not Ready

**Symptom**: Tests fail with connection timeout

**Solution**: The workflow includes a 30-attempt retry loop with health checks. If this still fails:
- Check GitHub Actions runner status
- Verify `SQL_SA_PASSWORD` meets complexity requirements
- Review SQL Server container logs in the workflow output

### DbSetup CLI Fails

**Symptom**: Database provisioning step fails

**Solution**:
1. Check that `SQL_SA_PASSWORD` is set correctly
2. Verify the DbSetup project builds successfully
3. Review the "Run DbSetup CLI" step output for specific errors
4. Ensure migrations are valid and don't have syntax errors

### Integration Tests Fail

**Symptom**: Tests run but fail with database errors

**Solution**:
1. Verify `DB_CONNECTION_STRING` format is correct
2. Check that migrations completed successfully
3. Review test logs for specific SQL errors
4. Ensure test data seeds correctly

### Secret Not Found

**Symptom**: Workflow fails with "secret not found" error

**Solution**:
```bash
# Verify secret exists
gh secret list

# Set the secret if missing
gh secret set SQL_SA_PASSWORD
```

## Environment Variables

The workflow sets the following environment variables:

| Variable | Value | Purpose |
|----------|-------|---------|
| `HOTSHOT_DB_SERVER` | `localhost` | SQL Server host |
| `HOTSHOT_DB_PORT` | `1433` | SQL Server port |
| `HOTSHOT_DB_NAME` | `hotshot_logistics` | Database name |
| `HOTSHOT_DB_APP_USER` | `hotshot_app` | Application user |
| `HOTSHOT_DB_APP_PASSWORD` | `${{ secrets.SQL_SA_PASSWORD }}` | App user password |
| `HOTSHOT_DB_SA_PASSWORD` | `${{ secrets.SQL_SA_PASSWORD }}` | SA password |

## Local Development vs CI/CD

### Local Development

```bash
# Use the DbSetup CLI with Docker
cd 7-Deployment/DbSetup/HotshotLogistics.DbSetup
dotnet run --project-slug "hotshot" --use-docker

# Or use docker-compose directly
cd 7-Deployment
docker-compose up -d
```

### CI/CD (GitHub Actions)

- **No Docker Compose needed**: Uses GitHub service containers
- **Non-interactive mode**: DbSetup runs without prompts
- **Ephemeral database**: Fresh database for each run
- **Parallel execution**: Multiple PRs can run tests simultaneously

## Cost and Performance

### GitHub Actions Minutes

- **Free tier**: 2,000 minutes/month for private repos
- **Public repos**: Unlimited
- **Typical workflow time**: 5-8 minutes
- **Concurrent runs**: Limited by your plan

### Optimization Tips

1. **Use concurrency groups**: Already configured to cancel redundant runs
2. **Cache .NET packages**: Included via `dotnet restore`
3. **Filter workflow triggers**: Only run on relevant file changes
4. **Skip on draft PRs**: Add `if: github.event.pull_request.draft == false`

## Advanced Configuration

### Running Only Integration Tests

```bash
# Filter by test project
dotnet test ./5-Test/HotshotLogistics.IntegrationTests/HotshotLogistics.IntegrationTests.csproj
```

### Using a Different Database Name

Edit the workflow:

```yaml
- name: Run DbSetup CLI
  run: |
    dotnet run --project-slug "hotshot" \
      --db-name "hotshot_logistics_ci" \
      ...
```

### Adding More Services

```yaml
services:
  sqlserver:
    # ... existing config

  redis:
    image: redis:alpine
    ports:
      - 6379:6379
```

## Monitoring and Reporting

### Test Results

Test results are automatically:
- Displayed in workflow logs
- Uploaded as artifacts (retained for 30 days)
- Summarized in the workflow step summary

### Viewing Test Results

1. Go to **Actions** tab in your repository
2. Click on a workflow run
3. Scroll to **Artifacts** section
4. Download `integration-test-results` ZIP file
5. Open the `.trx` file in Visual Studio or a TRX viewer

### GitHub Step Summary

The workflow automatically adds a summary to the GitHub Actions UI showing:
- Test execution status
- Artifact upload status

## Next Steps

1. **Set up the secret**: Add `SQL_SA_PASSWORD` to your repository
2. **Test the workflow**: Trigger it manually via `workflow_dispatch`
3. **Review test results**: Check the Actions tab for output
4. **Iterate and improve**: Adjust based on your needs

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [GitHub Secrets Guide](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [SQL Server on Linux Containers](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- [DbSetup CLI Documentation](../../7-Deployment/DbSetup/README.md)

## Support

If you encounter issues:
1. Check this guide's troubleshooting section
2. Review workflow logs in GitHub Actions
3. Verify all secrets are set correctly
4. Test DbSetup CLI locally to isolate issues
