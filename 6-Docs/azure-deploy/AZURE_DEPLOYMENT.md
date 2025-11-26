# Azure Deployment Quick Start Guide

This guide provides step-by-step instructions for deploying Hotshot Logistics to Azure using Pulumi.

## Quick Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Azure Architecture                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────┐         ┌──────────────────────┐      │
│  │  Static Web App     │         │   Container App      │      │
│  │  (Next.js Admin)    │◄───────►│   (.NET API)         │      │
│  │  Free Tier          │         │   Consumption        │      │
│  │                     │         │   Min: 0, Max: 3     │      │
│  └─────────────────────┘         └──────────────────────┘      │
│                                            │                     │
│                                            ▼                     │
│                                   ┌──────────────────┐          │
│                                   │   Azure SQL      │          │
│                                   │   Basic Tier     │          │
│                                   └──────────────────┘          │
│                                                                   │
│  ┌─────────────────────┐         ┌──────────────────────┐      │
│  │  Log Analytics      │◄────────│  App Insights        │      │
│  │  Workspace          │         │                      │      │
│  └─────────────────────┘         └──────────────────────┘      │
│                                                                   │
│  ┌─────────────────────────────────────────────────────┐        │
│  │         Azure Container Registry (ACR)              │        │
│  │         Stores Docker images                        │        │
│  └─────────────────────────────────────────────────────┘        │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Prerequisites Checklist

- [ ] Azure subscription (Owner or Contributor role)
- [ ] Pulumi CLI installed (bash: `curl -fsSL https://get.pulumi.com | sh`, PowerShell: `irm https://get.pulumi.com | iex`)
- [ ] .NET 8 SDK installed
- [ ] Azure CLI installed and logged in (`az login`)
- [ ] Docker installed (for local builds)
- [ ] Pulumi account at [app.pulumi.com](https://app.pulumi.com)
- [ ] GitHub repository with the code
- [ ] For Windows: WSL (Windows Subsystem for Linux) or Git Bash recommended for bash commands

## Step 1: Azure Setup (One-time)

### 1.1 Login to Azure

**Bash:**
```bash
az login
az account set --subscription <subscription-id>
```

**PowerShell:**
```powershell
az login
az account set --subscription <subscription-id>
```

### 1.2 Create Service Principal for CI/CD

**Bash:**
```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Create service principal
az ad sp create-for-rbac \
  --name "pulumi-hotshot-logistics" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth

# Save the output! You'll need:
# - clientId (AZURE_CLIENT_ID)
# - clientSecret (AZURE_CLIENT_SECRET)
# - subscriptionId (AZURE_SUBSCRIPTION_ID)
# - tenantId (AZURE_TENANT_ID)
```

**PowerShell:**
```powershell
# Get your subscription ID
$SUBSCRIPTION_ID = az account show --query id -o tsv

# Create service principal
az ad sp create-for-rbac `
  --name "pulumi-hotshot-logistics" `
  --role contributor `
  --scopes /subscriptions/$SUBSCRIPTION_ID `
  --sdk-auth

# Save the output! You'll need:
# - clientId (AZURE_CLIENT_ID)
# - clientSecret (AZURE_CLIENT_SECRET)
# - subscriptionId (AZURE_SUBSCRIPTION_ID)
# - tenantId (AZURE_TENANT_ID)
```

## Step 2: Pulumi Setup (One-time)

### 2.1 Install Pulumi

**Bash (Linux/macOS):**
```bash
# Install Pulumi CLI
curl -fsSL https://get.pulumi.com | sh

# Add to PATH
export PATH=$PATH:$HOME/.pulumi/bin

# Verify installation
pulumi version
```

**PowerShell (Windows):**
```powershell
# Install Pulumi CLI
irm https://get.pulumi.com | iex

# Pulumi installer adds itself to PATH automatically
# Verify installation
pulumi version
```

### 2.2 Create Pulumi Account

1. Go to [app.pulumi.com](https://app.pulumi.com)
2. Sign up/login
3. Go to Settings → Access Tokens
4. Create a new token and save it

### 2.3 Login to Pulumi

**Bash:**
```bash
# Set your access token
export PULUMI_ACCESS_TOKEN=<your-token>

# Login
pulumi login
```

**PowerShell:**
```powershell
# Set your access token
$env:PULUMI_ACCESS_TOKEN = "<your-token>"

# Login
pulumi login
```

## Step 3: Local Deployment (First Time)

### 3.1 Navigate to Pulumi Directory

**Bash:**
```bash
cd 7-Deployment/pulumi
```

**PowerShell:**
```powershell
cd 7-Deployment\pulumi
```

### 3.2 Restore Dependencies

```bash
dotnet restore
```

### 3.3 Initialize Stack

**Bash:**
```bash
# Create new dev stack
pulumi stack init dev

# Or select existing
pulumi stack select dev
```

**PowerShell:**
```powershell
# Create new dev stack
pulumi stack init dev

# Or select existing
pulumi stack select dev
```

### 3.4 Configure Stack

```bash
# Set location (choose your preferred region)
pulumi config set location eastus

# Set environment
pulumi config set environment dev

# Set SQL admin password (use a strong password!)
pulumi config set sqlAdminPassword --secret "YourStrongPassword123!"

# Optional: Set custom container image
# pulumi config set containerImage hotshot-api:latest
```

### 3.5 Preview and Deploy

```bash
# Preview what will be created
pulumi preview

# Deploy infrastructure
pulumi up

# Select 'yes' when prompted
```

### 3.6 Save Outputs

```bash
# Get all outputs
pulumi stack output --json > outputs.json

# Get specific values
pulumi stack output containerRegistryName
pulumi stack output containerAppUrl
pulumi stack output staticWebAppUrl
```

## Step 4: Build and Push Container Image

### 4.1 Option A: Use GitHub Actions (Recommended)

The easiest way to deploy is to use the combined infrastructure + container workflow:

```bash
# Go to GitHub Actions → Pulumi Infrastructure Deployment
# Select "Run workflow" with these options:
# - use_placeholder_image: false
# - build_and_push: true
```

This will:
1. Deploy infrastructure (if needed)
2. Build the Docker image using the chiseled base
3. Push to ACR using OIDC authentication (no secrets needed!)
4. Update the Container App with the new image

### 4.2 Option B: Manual Build and Push

```bash
# Get ACR name from Pulumi
ACR_NAME=$(pulumi stack output containerRegistryName)

# Login to ACR (uses your Azure CLI credentials)
az acr login --name $ACR_NAME

# Build the Docker image (uses chiseled Ubuntu base - smaller & more secure)
docker build -t $ACR_NAME.azurecr.io/hotshot-api:latest .

# Push to ACR
docker push $ACR_NAME.azurecr.io/hotshot-api:latest
```

### 4.3 Update Container App

```bash
# Get resource group and container app name
RG_NAME=$(pulumi stack output resourceGroupName)
CONTAINER_APP=$(pulumi stack output containerAppName)

# Update the container app
az containerapp update \
  --name $CONTAINER_APP \
  --resource-group $RG_NAME \
  --image $ACR_NAME.azurecr.io/hotshot-api:latest
```

### 4.4 Security Notes

**OIDC Authentication**: The workflows use OpenID Connect (OIDC) to authenticate with Azure. This means:
- No `ACR_USERNAME` or `ACR_PASSWORD` secrets needed
- No `AZURE_CLIENT_SECRET` needed for GitHub Actions
- Authentication is handled securely via federated identity

**Chiseled Container Image**: The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled`:
- ~100MB smaller than standard images
- No shell, no package manager (reduced attack surface)
- Non-root by default
- No apt/dpkg vulnerabilities

## Step 5: GitHub Actions Setup

### 5.1 Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Add these secrets:

**Azure Authentication (OIDC - Recommended):**

GitHub Actions uses **OpenID Connect (OIDC) authentication**. You do NOT need `AZURE_CLIENT_SECRET` or ACR credentials.

- `AZURE_CLIENT_ID` - From service principal output
- `AZURE_TENANT_ID` - From service principal output
- `AZURE_SUBSCRIPTION_ID` - From service principal output

**Important**: You need to set up OIDC federation in Azure AD. See [GitHub OIDC with Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure).

**Pulumi:**
- `PULUMI_ACCESS_TOKEN` - Your Pulumi access token

**Database:**
- `SQL_ADMIN_PASSWORD` - Same password you used in Pulumi config

**Static Web App:**
```bash
# Get deployment token (requires --show-secrets flag)
pulumi stack output staticWebAppDeploymentToken --show-secrets
```
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - Deployment token

**Azure AD / Entra External ID (for MSAL Authentication):**

The admin dashboard uses MSAL with Azure Entra ID (formerly Azure AD B2C) for authentication.
The app code expects `NEXT_PUBLIC_AZURE_CLIENT_ID` and `NEXT_PUBLIC_AZURE_TENANT_ID`, but GitHub
secrets use the naming convention without the `NEXT_PUBLIC_` prefix (the workflow maps them).

- `AZURE_AD_B2C_CLIENT_ID` - Your Azure AD App Registration Client ID (GUID)
- `AZURE_AD_B2C_TENANT_ID` - Your Azure AD Tenant ID (GUID)

To get these values:
1. Go to Azure Portal → Azure Active Directory → App registrations
2. Select your app (or create one following `6-Docs/admin-dashboard/AZURE_AD_SETUP.md`)
3. Copy the **Application (client) ID** → `AZURE_AD_B2C_CLIENT_ID`
4. Copy the **Directory (tenant) ID** → `AZURE_AD_B2C_TENANT_ID`

**API Configuration:**
```bash
# Get the Container App URL
pulumi stack output containerAppFullUrl
```
- `NEXT_PUBLIC_API_URL` - Container App URL (e.g., `https://ca-hotshot-api-dev.<region>.azurecontainerapps.io`)
  - This can be set as either a Secret or Variable in GitHub
  - Required for CSP (Content Security Policy) to allow API calls from the dashboard

### 5.2 (Optional) Configure GitHub Variables

Go to Settings → Secrets and variables → Actions → Variables tab

These are optional fallbacks if Pulumi outputs aren't available:
- `ACR_NAME` - Your ACR name (e.g., `crhotshotdevabcd1234`)
- `RESOURCE_GROUP_NAME` - Your resource group name (e.g., `rg-hotshot-dev`)

### 5.2 Update Workflow Files

Edit `.github/workflows/build-and-push-container.yml`:

```yaml
env:
  REGISTRY_NAME: <your-acr-name>  # e.g., crhotshotdev
```

### 5.3 Trigger Workflows

```bash
# Push to main branch to trigger deployment
git add .
git commit -m "Configure Azure deployment"
git push origin main
```

## Step 6: Verify Deployment

### 6.1 Check Container App

```bash
# Get the URL
APP_URL=$(pulumi stack output containerAppUrl)

# Test the API
curl https://$APP_URL/health
```

### 6.2 Check Static Web App

```bash
# Get the URL
WEB_URL=$(pulumi stack output staticWebAppUrl)

# Open in browser
echo "Admin Dashboard: https://$WEB_URL"
```

### 6.3 Check Logs

```bash
# Container App logs
az containerapp logs show \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME \
  --follow

# Or view in Azure Portal
az containerapp show \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME \
  --query properties.configuration.ingress.fqdn -o tsv
```

## Step 7: Database Migration

### 7.1 Get Connection String

```bash
# Get SQL Server details
SQL_SERVER=$(pulumi stack output sqlServerFqdn)
DB_NAME=$(pulumi stack output databaseName)

# Build connection string
echo "Server=tcp:$SQL_SERVER,1433;Initial Catalog=$DB_NAME;User ID=sqladmin;Password=<your-password>;Encrypt=True;"
```

### 7.2 Run Migrations

```bash
# Set connection string
export DB_CONNECTION_STRING="Server=tcp:$SQL_SERVER,1433;Initial Catalog=$DB_NAME;User ID=sqladmin;Password=<your-password>;Encrypt=True;"

# Navigate to migration runner
cd 4-Persistence/MigrationRunner

# Run migrations
dotnet run
```

## Common Tasks

### Update Infrastructure

```bash
cd 7-Deployment/pulumi

# Make changes to Program.cs
# ...

# Preview changes
pulumi preview

# Apply changes
pulumi up
```

### Scale Container App

```bash
# Update in Program.cs:
Scale = new ScaleArgs
{
    MinReplicas = 1,  # Change from 0
    MaxReplicas = 5   # Change from 3
}

# Apply
pulumi up
```

### View Costs

```bash
# Azure Portal → Cost Management → Cost Analysis

# Or via CLI
az consumption usage list \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --query "[?contains(instanceName,'hotshot')]"
```

### Rollback Container Image

```bash
# List revisions
az containerapp revision list \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME \
  --query "[].{Name:name, Active:properties.active, Created:properties.createdTime}" \
  -o table

# Activate previous revision
az containerapp revision activate \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME \
  --revision <revision-name>
```

### Destroy Everything

```bash
cd 7-Deployment/pulumi

# Preview what will be destroyed
pulumi destroy --preview-only

# Destroy all resources
pulumi destroy

# Confirm with 'yes'
```

## Troubleshooting

### Container App Not Starting

```bash
# Check logs
az containerapp logs show \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME \
  --follow

# Check system logs
az containerapp logs show \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME \
  --type system \
  --follow
```

### SQL Connection Issues

```bash
# Add your IP to firewall
MY_IP=$(curl -s ifconfig.me)
az sql server firewall-rule create \
  --resource-group $RG_NAME \
  --server <sql-server-name> \
  --name "AllowMyIP" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP
```

### Static Web App Not Deploying

1. Check deployment token is correct
2. Verify build output directory is `out`
3. Check GitHub Actions logs
4. Ensure `output: 'export'` is set in `next.config.ts`

### Pulumi State Issues

```bash
# Refresh state
pulumi refresh

# Cancel stuck update
pulumi cancel

# Export state (backup)
pulumi stack export > backup.json

# Import state (restore)
pulumi stack import < backup.json
```

## Cost Optimization

### Development Environment

```bash
# Stop Container App (scales to 0 automatically with min: 0)
# No action needed - Container Apps with min: 0 auto-scale to zero

# Or delete the Container App (keep infrastructure)
az containerapp delete \
  --name ca-hotshot-api-dev \
  --resource-group $RG_NAME

# Pause SQL Database (Azure Portal → SQL Database → Overview → Pause)
# Or via CLI
az sql db pause \
  --name $DB_NAME \
  --resource-group $RG_NAME \
  --server <sql-server-name>
```

### Resume Development

```bash
# Resume SQL Database
az sql db resume \
  --name $DB_NAME \
  --resource-group $RG_NAME \
  --server <sql-server-name>

# Container App auto-starts on first request (if min: 0)
```

## Production Deployment

For production, create a new stack:

```bash
pulumi stack init prod
pulumi config set location eastus
pulumi config set environment prod
pulumi config set sqlAdminPassword --secret "<strong-password>"

# Adjust scaling for production
# Edit Program.cs to set higher min/max replicas
# minReplicas: 1 or 2
# maxReplicas: 10 or more

pulumi up
```

## Next Steps

1. Configure custom domain for Static Web App
2. Set up SSL certificates
3. Configure Azure AD B2C authentication
4. Set up monitoring alerts in Application Insights
5. Configure backup for SQL Database
6. Set up staging environment
7. Configure deployment slots for zero-downtime deployments

## Resources

- [Pulumi README](pulumi/README.md) - Detailed infrastructure documentation
- [Container Apps Docs](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Static Web Apps Docs](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Pulumi Azure Native](https://www.pulumi.com/registry/packages/azure-native/)
