# Azure Infrastructure Deployment with Pulumi

This directory contains the Pulumi infrastructure-as-code configuration for deploying Hotshot Logistics to Azure.

## Architecture Overview

The infrastructure deploys the following Azure resources:

### Core Components

- **Container Apps Environment** (Consumption only - Workload Profiles v2)
  - Consumption-based pricing
  - Integrated with Log Analytics for observability
  - Auto-scaling with KEDA HTTP scaler

- **Container App** (API Backend)
  - Hosts the .NET 8 ASP.NET Core Web API
  - Scaling: `minReplicas: 0`, `maxReplicas: 3`
  - HTTP scaling based on concurrent requests (KEDA default)
  - Exposes port 8080 with external ingress

- **Azure Container Registry** (ACR)
  - Basic SKU
  - Stores the API container images
  - Admin user enabled for CI/CD

- **Static Web Apps** (Free tier)
  - Hosts the Next.js admin dashboard
  - Static export build (`npm run build` → `out/`)
  - Global CDN distribution

### Database

- **Azure SQL Server**
  - SQL Server 12.0 with TLS 1.2 minimum
  - Basic tier database (2GB)
  - Firewall rule to allow Azure services

### Observability

- **Log Analytics Workspace**
  - 30-day retention
  - Pay-as-you-go pricing
  - Linked to Container Apps and App Insights

- **Application Insights**
  - Connected to Log Analytics workspace
  - Monitors API performance and errors
  - Low volume, pay-as-you-go

## Prerequisites

### Required Software

1. **Pulumi CLI** (v3.0+)

   **macOS/Linux:**
   ```bash
   curl -fsSL https://get.pulumi.com | sh
   ```

   **Windows (PowerShell):**
   ```powershell
   irm https://get.pulumi.com | iex
   ```

2. **.NET SDK** (8.0+)

   **macOS/Linux:**
   ```bash
   dotnet --version
   ```

   **Windows (PowerShell):**
   ```powershell
   dotnet --version
   ```

3. **Azure CLI**

   **macOS/Linux:**
   ```bash
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   az login
   ```

   **Windows (PowerShell):**
   ```powershell
   # Using winget (Windows Package Manager)
   winget install Microsoft.AzureCLI
   az login
   ```

   > **Note:** Windows users can also use WSL (Windows Subsystem for Linux) with bash commands

4. **Pulumi Account**
   - Create a free account at [app.pulumi.com](https://app.pulumi.com)
   - Get your access token from Settings → Access Tokens

### Azure Setup

1. **Azure Subscription**
   - Active Azure subscription with Owner or Contributor role
   - Note your subscription ID

2. **Service Principal** (for CI/CD)
   ```bash
   # Create service principal with contributor role
   az ad sp create-for-rbac \
     --name "pulumi-hotshot-logistics" \
     --role contributor \
     --scopes /subscriptions/<subscription-id> \
     --sdk-auth
   ```

   Save the output (client ID, tenant ID, subscription ID, client secret).

## Local Setup

### 1. Install Dependencies

```bash
cd 7-Deployment/pulumi
dotnet restore
```

### 2. Login to Pulumi

```bash
# Login using your Pulumi access token
pulumi login

# Or login to Pulumi cloud
pulumi login --cloud-url https://app.pulumi.com
```

### 3. Initialize Stack

```bash
# Create a new stack (e.g., dev, staging, prod)
pulumi stack init dev

# Or select an existing stack
pulumi stack select dev
```

### 4. Configure Stack

```bash
# Set Azure region
pulumi config set location eastus

# Set environment name
pulumi config set environment dev

# Set SQL admin login (optional, defaults to "sqladmin")
pulumi config set sqlAdminLogin myadmin

# Set SQL firewall allowed IP ranges (optional, defaults to "0.0.0.0" for Azure services)
# Use comma-separated IP addresses for multiple ranges
# For production, specify known IP ranges or use private endpoints
pulumi config set sqlAllowedIpRanges "52.123.45.67,52.123.45.68"

# Set container image (optional, defaults to hello-world)
# IMPORTANT: Must be a fully qualified image name
# Do not use partial names - they will not be prefixed with ACR login server
pulumi config set containerImage mcr.microsoft.com/azuredocs/containerapps-helloworld:latest
```

### 5. Set Required Environment Variables

All sensitive values are supplied via environment variables (GitHub Actions secrets in CI). Before running `pulumi up` locally, export the following variables:

```
# PowerShell example
$env:SQL_ADMIN_PASSWORD = "<strong-password>"
$env:AZURE_TENANT_ID = "<tenant-guid>"
$env:AZURE_SUBSCRIPTION_ID = "<subscription-guid>"
$env:AZURE_AD_B2C_INSTANCE = "<tenant>.b2clogin.com" # can omit https://
$env:AZURE_AD_B2C_CLIENT_ID = "<app-guid>"
$env:AZURE_AD_B2C_DOMAIN = "<b2c-domain>"
$env:AZURE_AD_B2C_TENANT_ID = "<b2c-tenant-guid>"
$env:AZURE_AD_B2C_AUDIENCE = "<api-audience>"
```

> **Important:** Secrets are no longer stored in Pulumi stack config. Local runs or CI/CD executions will fail fast if any variable is missing.

### 6. Deploy Infrastructure

```bash
# Preview changes
pulumi preview

# Deploy infrastructure
pulumi up
```

### 7. View Outputs

```bash
# Show all stack outputs
pulumi stack output

# Get specific output
pulumi stack output containerAppUrl
pulumi stack output staticWebAppUrl
```

## GitHub Actions CI/CD Setup

### Required GitHub Secrets

Configure these secrets in your GitHub repository (Settings → Secrets and variables → Actions):

#### Azure Authentication

GitHub Actions uses **OpenID Connect (OIDC) authentication** instead of client secrets. No `AZURE_CLIENT_SECRET` is required.

- `AZURE_CLIENT_ID` - Service principal client ID
- `AZURE_TENANT_ID` - Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID
- For local development (non-GitHub), you may need `AZURE_CLIENT_SECRET` to authenticate with Azure

#### Pulumi
- `PULUMI_ACCESS_TOKEN` - Pulumi access token from app.pulumi.com

#### Database
- `SQL_ADMIN_PASSWORD` - Strong password for SQL Server admin

#### Azure AD B2C
- `AZURE_AD_B2C_INSTANCE` - B2C instance (with or without `https://`)
- `AZURE_AD_B2C_CLIENT_ID` - SPA/client application ID
- `AZURE_AD_B2C_DOMAIN` - B2C domain (e.g., `contoso.onmicrosoft.com`)
- `AZURE_AD_B2C_TENANT_ID` - B2C directory/tenant ID
- `AZURE_AD_B2C_AUDIENCE` - API application ID URI / audience value

#### Container Registry (set after first deployment)
- `ACR_USERNAME` - ACR admin username (from `pulumi stack output`)
- `ACR_PASSWORD` - ACR admin password (from Azure portal or CLI)

#### Static Web App
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - Deployment token (get with `pulumi stack output staticWebAppDeploymentToken --show-secrets`)

#### API Configuration
- `NEXT_PUBLIC_API_URL` - Container App URL for API calls (e.g., `https://ca-hotshot-api-dev.eastus.azurecontainerapps.io`)
  - Required for Next.js build to configure CSP (Content Security Policy)
  - Get with: `pulumi stack output containerAppUrl`
  - Can be set as either a Secret or Variable in GitHub Actions

### Configuring GitHub Workflows for ACR

The `build-and-push-container.yml` workflow uses the ACR registry name. By default, it uses `crhotshotdev`, but you can configure it via a GitHub repository variable:

1. **Set the `ACR_NAME` variable** (optional if using default):
   ```bash
   # Get your ACR name from Pulumi output after deployment
   pulumi stack output containerRegistryName
   ```
   - Go to GitHub repository → Settings → Variables and secrets → Variables
   - Create a new variable `ACR_NAME` with your ACR name (e.g., `crhotshotdev`)

### Getting ACR Credentials

After the first Pulumi deployment:

```bash
# Get ACR credentials
az acr credential show --name <registry-name>

# Or via Pulumi
pulumi stack output containerRegistryName
az acr credential show --name $(pulumi stack output containerRegistryName)
```

### Workflows

1. **`pulumi-deploy.yml`** - Deploys infrastructure
   - Triggers on push to `main` or manual workflow dispatch
   - Previews changes before deployment
   - Deploys only to dev environment automatically

2. **`build-and-push-container.yml`** - Builds and pushes Docker images
   - Triggers on code changes or manual dispatch
   - Builds the API container image
   - Pushes to Azure Container Registry
   - Tags: `latest`, `<branch>-<sha>`, `<branch>`

## Stack Outputs

After deployment, Pulumi exports these outputs:

| Output | Description |
|--------|-------------|
| `resourceGroupName` | Name of the Azure resource group |
| `containerRegistryName` | ACR name |
| `containerRegistryLoginServer` | ACR login URL |
| `containerAppUrl` | API endpoint URL |
| `staticWebAppUrl` | Admin dashboard URL |
| `staticWebAppDeploymentToken` | Deployment token for Static Web Apps |
| `appInsightsInstrumentationKey` | Application Insights key |
| `appInsightsConnectionString` | Application Insights connection string |
| `sqlServerFqdn` | SQL Server fully qualified domain name |
| `databaseName` | SQL Database name |
| `logAnalyticsWorkspaceId` | Log Analytics workspace ID |

## Updating Infrastructure

### Modify Resources

1. Edit `Program.cs` with your changes
2. Run `pulumi preview` to see the diff
3. Run `pulumi up` to apply changes

### Add New Environment Variables

To add environment variables to the Container App:

```csharp
new EnvironmentVarArgs
{
    Name = "NEW_ENV_VAR",
    Value = "value"
}
```

For secrets:

```csharp
// Add to Secrets array
new SecretArgs
{
    Name = "my-secret",
    Value = config.RequireSecret("mySecret")
}

// Reference in environment variables
new EnvironmentVarArgs
{
    Name = "MY_SECRET",
    SecretRef = "my-secret"
}
```

### Configure CORS for Production

The API supports CORS configuration via environment variables. You can configure allowed origins in two ways:

**Option 1: Comma-separated string (recommended for environment variables)**
```csharp
new EnvironmentVarArgs
{
    Name = "Cors__AllowedOrigins",
    Value = "https://app.example.com,https://admin.example.com"
}
```

**Option 2: JSON array in appsettings.json**
```json
{
  "Cors": {
    "AllowedOrigins": ["https://app.example.com", "https://admin.example.com"]
  }
}
```

The API automatically handles both formats. When using environment variables, separate multiple origins with commas (no spaces needed - they will be trimmed automatically).

## Manual Container Deployment

To manually update the container image:

```bash
# Build and tag the image
docker build -t <registry>.azurecr.io/hotshot-api:latest .

# Login to ACR
az acr login --name <registry>

# Push the image
docker push <registry>.azurecr.io/hotshot-api:latest

# Update the Container App
az containerapp update \
  --name ca-hotshot-api-dev \
  --resource-group rg-hotshot-dev \
  --image <registry>.azurecr.io/hotshot-api:latest
```

## Troubleshooting

### Pulumi Login Issues

```bash
# If you have issues logging in
pulumi logout
pulumi login
```

### Azure Authentication Issues

```bash
# Verify Azure login
az account show

# Set the correct subscription
az account set --subscription <subscription-id>
```

### Container App Not Starting

1. Check logs:
   ```bash
   az containerapp logs show \
     --name ca-hotshot-api-dev \
     --resource-group rg-hotshot-dev \
     --follow
   ```

2. Check revisions:
   ```bash
   az containerapp revision list \
     --name ca-hotshot-api-dev \
     --resource-group rg-hotshot-dev
   ```

### Static Web App Deployment Issues

Check the deployment token:
```bash
pulumi stack output staticWebAppDeploymentToken
```

### Database Connection Issues

1. Verify firewall rules allow your IP
2. Check connection string format
3. Ensure SQL admin credentials are correct

## Cost Optimization

### Current Configuration Costs (Approximate)

- **Container Apps (Consumption)**: Pay per use, scales to zero
  - ~$0.00001 per vCPU-second
  - ~$0.000002 per GiB-second
  - Free tier: 180,000 vCPU-seconds + 360,000 GiB-seconds per month

- **Static Web Apps (Free)**: $0/month
  - 100GB bandwidth/month included
  - Custom domains included

- **SQL Database (Basic)**: ~$5/month
  - 2GB storage included

- **Container Registry (Basic)**: ~$5/month
  - 10GB storage included

- **Log Analytics**: Pay-as-you-go
  - First 5GB/month free
  - $2.30/GB after that

- **Application Insights**: Pay-as-you-go
  - First 5GB/month free

**Estimated monthly cost**: ~$10-15 for dev environment with low traffic

### Cost Reduction Tips

1. **Use dev/test pricing** for non-production databases
2. **Delete unused resources** with `pulumi destroy`
3. **Scale down during off-hours** (configure scaling rules)
4. **Monitor costs** in Azure Cost Management

## Cleanup

To destroy all infrastructure:

```bash
# Preview what will be destroyed
pulumi destroy --preview-only

# Destroy all resources
pulumi destroy

# Remove the stack
pulumi stack rm dev
```

## Security Best Practices

1. **Never commit secrets** - Use Pulumi secrets or GitHub secrets
2. **Use managed identities** where possible
3. **Enable Azure AD authentication** for databases
4. **Restrict network access** with firewall rules
5. **Enable Azure Security Center** recommendations
6. **Rotate secrets regularly** (SQL passwords, ACR credentials)
7. **Use Key Vault** for production secrets

## Additional Resources

- [Pulumi Azure Native Documentation](https://www.pulumi.com/registry/packages/azure-native/)
- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure Static Web Apps Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [KEDA HTTP Add-on](https://learn.microsoft.com/en-us/azure/container-apps/scale-app)

## Support

For issues with:
- **Pulumi**: [Pulumi Community Slack](https://slack.pulumi.com/)
- **Azure**: [Azure Support](https://azure.microsoft.com/support/)
- **This project**: Open an issue in the GitHub repository
