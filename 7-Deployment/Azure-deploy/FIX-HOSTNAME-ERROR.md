# Fix: "Bad Request - Invalid Hostname" Error

## Problem

After deploying the Azure Container App, accessing the API results in:

```
Bad Request - Invalid Hostname
HTTP Error 400. The request hostname is invalid.
```

## Root Cause

The API's [`appsettings.json`](../../1-Presentation/HotshotLogistics.Api/appsettings.json) and [`appsettings.Production.json`](../../1-Presentation/HotshotLogistics.Api/appsettings.Production.json) reference environment variables:

```json
"AllowedHosts": "${ALLOWED_HOSTS}",
"Cors": {
  "AllowedOrigins": "${CORS_ALLOWED_ORIGINS}"
}
```

However, these environment variables were **NOT** being set during deployment, causing ASP.NET Core to use the literal string `"${ALLOWED_HOSTS}"`, which is not a valid hostname.

ASP.NET Core validates the `Host` header against `AllowedHosts`, and when it doesn't match, it returns a 400 error.

## Solution (Automated)

The deployment pipeline has been updated to **automatically configure** the Container App environment variables after infrastructure deployment:

### What Was Fixed

1. **Updated [`7-Deployment/Azure-deploy/Program.cs`](./Program.cs)**
   - Sets initial `ALLOWED_HOSTS` and `CORS_ALLOWED_ORIGINS` to prevent deployment failures
   - Uses Pulumi config values or sensible defaults

2. **Updated [`.github/workflows/pulumi-deploy.yml`](../../.github/workflows/pulumi-deploy.yml)**
   - Added **"Configure Container App Environment Variables"** step (after line 232)
   - Automatically retrieves actual Container App FQDN and Static Web App URL after deployment
   - Updates Container App environment variables using `az containerapp update`
   - Restarts Container App to apply changes
   - **No manual intervention required!**

### The Automated Workflow

```yaml
- name: Configure Container App Environment Variables
  run: |
    # Get deployed URLs
    CONTAINER_APP_FQDN=$(echo "$CONTAINER_APP_URL" | sed 's|https://||')
    
    # Update Container App environment variables
    az containerapp update \
      --name "$CONTAINER_APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --set-env-vars \
        "ALLOWED_HOSTS=$CONTAINER_APP_FQDN" \
        "CORS_ALLOWED_ORIGINS=$STATIC_WEB_APP_URL,$CONTAINER_APP_URL,http://localhost:3000"
    
    # Restart to apply changes
    az containerapp revision restart \
      --name "$CONTAINER_APP_NAME" \
      --resource-group "$RESOURCE_GROUP"
```

## How to Apply the Fix

### For New Deployments

**No action required!** The GitHub Actions workflow automatically configures everything:

1. Push your code or trigger the workflow manually
2. Infrastructure is deployed
3. Environment variables are automatically configured
4. Container App is automatically restarted
5. API is ready to use!

### For Existing Deployments (Manual Fix)

If you already have a deployed Container App with this issue:

**Quick Fix via Azure CLI:**

```bash
# Get resource information from Pulumi
cd 7-Deployment/Azure-deploy
RG_NAME=$(pulumi stack output resourceGroupName)
APP_NAME=$(pulumi stack output containerAppName)
CONTAINER_APP_URL=$(pulumi stack output containerAppFullUrl)
SWA_URL=$(pulumi stack output staticWebAppFullUrl)
CONTAINER_APP_FQDN=$(echo "$CONTAINER_APP_URL" | sed 's|https://||')

# Update environment variables
az containerapp update \
  --name "$APP_NAME" \
  --resource-group "$RG_NAME" \
  --set-env-vars \
    "ALLOWED_HOSTS=$CONTAINER_APP_FQDN" \
    "CORS_ALLOWED_ORIGINS=$SWA_URL,$CONTAINER_APP_URL,http://localhost:3000"

# Restart to apply
az containerapp revision restart \
  --name "$APP_NAME" \
  --resource-group "$RG_NAME"

echo "✅ Environment variables configured and Container App restarted"
```

## Verification

After deployment or applying the fix:

```bash
# Get the Container App URL
APP_URL=$(pulumi stack output containerAppFullUrl)

# Test the API (should return 200 or 307 redirect)
curl -I $APP_URL

# Test Swagger endpoint
curl $APP_URL/swagger/v1/swagger.json
```

**Expected behavior:**
- ✅ No "Bad Request - Invalid Hostname" error
- ✅ API returns valid responses
- ✅ Swagger JSON is accessible

## What's Configured Automatically

The deployment pipeline now sets:

| Variable | Value | Purpose |
|----------|-------|---------|
| `ALLOWED_HOSTS` | `ca-hotshot-api-{env}.{region}.azurecontainerapps.io` | ASP.NET Core Host validation |
| `CORS_ALLOWED_ORIGINS` | `https://{swa-url},https://{api-url},http://localhost:3000` | CORS policy for frontend access |

## Benefits

✅ **Fully Automated** - No manual steps required after deployment  
✅ **Self-Configuring** - Uses actual deployed URLs, not predictions  
✅ **Zero Downtime** - Container App restart is seamless  
✅ **Development Friendly** - Includes localhost for local testing  
✅ **CI/CD Ready** - Works in GitHub Actions pipelines  

## Related Files

- [`.github/workflows/pulumi-deploy.yml`](../../.github/workflows/pulumi-deploy.yml) - Deployment pipeline (FIXED)
- [`7-Deployment/Azure-deploy/Program.cs`](./Program.cs) - Pulumi infrastructure code (UPDATED)
- [`1-Presentation/HotshotLogistics.Api/appsettings.json`](../../1-Presentation/HotshotLogistics.Api/appsettings.json) - API configuration
- [`1-Presentation/HotshotLogistics.Api/Program.cs`](../../1-Presentation/HotshotLogistics.Api/Program.cs) - API startup code