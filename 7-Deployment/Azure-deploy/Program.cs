using System.Collections.Generic;
using System;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.ApplicationInsights;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Sql.Inputs;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.KeyVault.Inputs;
using Pulumi.AzureNative.AppConfiguration;
using Pulumi.AzureNative.ManagedIdentity;
using Pulumi.AzureNative.Authorization;
using System.Linq;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Pulumi.Config();
    var location = config.Get("location") ?? "eastus";
    var environment = config.Get("environment") ?? "dev";
    var sqlAdminLogin = config.Get("sqlAdminLogin") ?? "sqladmin";
    var sqlAdminPassword = config.RequireSecret("sqlAdminPassword");
    
    // Compute the expected Container App and Static Web App FQDNs
    // These follow Azure's naming conventions and can be predicted before resource creation
    var containerAppFqdn = $"ca-hotshot-api-{environment}.{location}.azurecontainerapps.io";
    var staticWebAppRegion = location == "eastus2" ? "eastus2" : "centralus"; // Static Web Apps have limited regions
    var staticWebAppName = $"swa-hotshot-{environment}";
    // Azure AD Tenant ID is required for Key Vault access policies
    // Get from: az account show --query tenantId -o tsv
    var azureTenantId = config.Require("azureTenantId");
    // Azure Subscription ID is required for role assignments
    // Get from: az account show --query id -o tsv
    var subscriptionId = config.Require("subscriptionId");
    // Container image name (without registry prefix). Default: hotshot-api
    // The full image path will be constructed as: <registry>.azurecr.io/<imageName>:<imageTag>
    var imageName = config.Get("imageName") ?? "hotshot-api";
    var imageTag = config.Get("imageTag") ?? "latest";
    // Set to true to use a placeholder image for initial deployment before pushing your own image
    var usePlaceholderImage = config.GetBoolean("usePlaceholderImage") ?? false;
    // SQL firewall allowed IP ranges (comma-separated). If not specified, defaults to Azure services only (0.0.0.0)
    // For production, specify known IP ranges or use private endpoints instead
    var sqlAllowedIpRanges = config.Get("sqlAllowedIpRanges") ?? "0.0.0.0";
    
    // Azure AD B2C / Entra External ID configuration for API authentication
    // These must be configured in Pulumi config or the API will fail to start
    var azureAdB2cInstance = config.Get("azureAdB2cInstance") ?? "";
    var azureAdB2cClientId = config.Get("azureAdB2cClientId") ?? "";
    var azureAdB2cDomain = config.Get("azureAdB2cDomain") ?? "";
    var azureAdB2cTenantId = config.Get("azureAdB2cTenantId") ?? "";
    var azureAdB2cAudience = config.Get("azureAdB2cAudience") ?? "";

    // Resource Group
    var resourceGroup = new ResourceGroup($"rg-hotshot-{environment}", new ResourceGroupArgs
    {
        Location = location,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" },
            { "ManagedBy", "Pulumi" }
        }
    });

    // Log Analytics Workspace
    var workspace = new Workspace($"log-hotshot-{environment}", new WorkspaceArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        RetentionInDays = 30,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Retrieve the Log Analytics Workspace shared keys
    var workspaceSharedKeys = Output.Tuple(resourceGroup.Name, workspace.Name).Apply(t =>
        GetSharedKeys.InvokeAsync(new GetSharedKeysArgs
        {
            ResourceGroupName = t.Item1,
            WorkspaceName = t.Item2
        }));

    var workspaceSharedKey = Output.CreateSecret(workspaceSharedKeys.Apply(k => k.PrimarySharedKey ?? ""));

    // Application Insights Component
    var appInsights = new Component($"appi-hotshot-{environment}", new ComponentArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        ApplicationType = "web",
        Kind = "web",
        WorkspaceResourceId = workspace.Id,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    var appInsightsInstrumentationKey = appInsights.InstrumentationKey;
    var appInsightsConnectionString = appInsights.ConnectionString;

    // Managed Identity for Container App
    // This identity is used for authentication with Container Registry instead of admin credentials
    var containerAppIdentity = new UserAssignedIdentity($"id-hotshot-{environment}", new UserAssignedIdentityArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Container Registry with AdminUserEnabled DISABLED for enhanced security
    var registry = new Registry($"crhotshot{environment}", new RegistryArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new Pulumi.AzureNative.ContainerRegistry.Inputs.SkuArgs
        {
            Name = "Basic"
        },
        AdminUserEnabled = false, // SECURITY: Disabled admin user - use managed identity instead
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Assign AcrPull role to managed identity for registry access
    // This allows the container app to pull images without admin credentials
    // AcrPull role definition ID: 7f951dda-4ed3-4680-a7ca-43fe172d538d
    var acrPullRoleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";

    var acrPullRoleAssignment = new RoleAssignment($"acr-pull-{environment}", new RoleAssignmentArgs
    {
        PrincipalId = containerAppIdentity.PrincipalId,
        PrincipalType = Pulumi.AzureNative.Authorization.PrincipalType.ServicePrincipal,
        RoleDefinitionId = acrPullRoleDefinitionId,
        Scope = registry.Id
    });

    // Azure Key Vault for secrets (using RBAC for access control)
    var keyVault = new Vault($"kv-hotshot-{environment}", new VaultArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Properties = new VaultPropertiesArgs
        {
            TenantId = azureTenantId,
            Sku = new Pulumi.AzureNative.KeyVault.Inputs.SkuArgs
            {
                Family = "A",
                Name = Pulumi.AzureNative.KeyVault.SkuName.Standard
            },
            EnableRbacAuthorization = true,
            EnabledForDeployment = true,
            EnabledForTemplateDeployment = true
            // Note: EnableSoftDelete and SoftDeleteRetentionInDays cannot be changed after creation
            // Defaults: EnableSoftDelete=true, SoftDeleteRetentionInDays=90
        },
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Key Vault Secrets User role for managed identity (4633458b-17de-408a-b874-0445c86b69e6)
    var kvSecretsUserRoleId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/4633458b-17de-408a-b874-0445c86b69e6";
    var kvSecretsRoleAssignment = new RoleAssignment($"kv-secrets-{environment}", new RoleAssignmentArgs
    {
        PrincipalId = containerAppIdentity.PrincipalId,
        PrincipalType = Pulumi.AzureNative.Authorization.PrincipalType.ServicePrincipal,
        RoleDefinitionId = kvSecretsUserRoleId,
        Scope = keyVault.Id
    });

    // Azure App Configuration for non-secret configuration
    // Note: Free tier can take 5-10 minutes to provision
    var appConfig = new ConfigurationStore($"appcs-hotshot-{environment}", new ConfigurationStoreArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new Pulumi.AzureNative.AppConfiguration.Inputs.SkuArgs
        {
            Name = "free"
        },
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    }, new CustomResourceOptions
    {
        CustomTimeouts = new CustomTimeouts
        {
            Create = TimeSpan.FromMinutes(15)
        }
    });

    // App Configuration Data Reader role for managed identity (516239f1-63e1-4d78-a4de-a74fb236a071)
    var appConfigDataReaderRoleId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/516239f1-63e1-4d78-a4de-a74fb236a071";
    var appConfigRoleAssignment = new RoleAssignment($"appconfig-reader-{environment}", new RoleAssignmentArgs
    {
        PrincipalId = containerAppIdentity.PrincipalId,
        PrincipalType = Pulumi.AzureNative.Authorization.PrincipalType.ServicePrincipal,
        RoleDefinitionId = appConfigDataReaderRoleId,
        Scope = appConfig.Id
    });

    // SQL Server
    var sqlServer = new Server($"sql-hotshot-{environment}", new ServerArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        AdministratorLogin = sqlAdminLogin,
        AdministratorLoginPassword = sqlAdminPassword,
        Version = "12.0",
        MinimalTlsVersion = "1.2",
        PublicNetworkAccess = "Enabled",
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // SQL Database
    var database = new Database($"sqldb-hotshot-{environment}", new DatabaseArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        ServerName = sqlServer.Name,
        Sku = new Pulumi.AzureNative.Sql.Inputs.SkuArgs
        {
            Name = "Basic",
            Tier = "Basic"
        },
        MaxSizeBytes = 2147483648, // 2GB
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Firewall rule to allow Azure services or specified IP ranges
    // NOTE: 0.0.0.0 is a special rule that allows Azure services to access the server
    // For production environments, consider using private endpoints or restricting to known IP ranges
    var firewallRules = new List<FirewallRule>();
    var ipRanges = sqlAllowedIpRanges.Split(',');
    for (int i = 0; i < ipRanges.Length; i++)
    {
        var ipRange = ipRanges[i].Trim();
        var ruleName = ipRange == "0.0.0.0"
            ? $"sqlfw-azure-services-{environment}"
            : $"sqlfw-allowed-{i}-{environment}";

        firewallRules.Add(new FirewallRule(ruleName, new FirewallRuleArgs
        {
            ResourceGroupName = resourceGroup.Name,
            ServerName = sqlServer.Name,
            StartIpAddress = ipRange,
            EndIpAddress = ipRange
        }));
    }

    // Build connection string (marked as secret to prevent exposure in state)
    var connectionString = Output.CreateSecret(
        Output.Tuple(sqlServer.FullyQualifiedDomainName, database.Name, sqlAdminPassword)
            .Apply(t => $"Server=tcp:{t.Item1},1433;Initial Catalog={t.Item2};Persist Security Info=False;User ID={sqlAdminLogin};Password={t.Item3};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"));

    // Container Apps Environment (Consumption only - Workload Profiles v2)
    var managedEnvironment = new ManagedEnvironment($"cae-hotshot-{environment}", new ManagedEnvironmentArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        AppLogsConfiguration = new Pulumi.AzureNative.App.Inputs.AppLogsConfigurationArgs
        {
            Destination = "log-analytics",
            LogAnalyticsConfiguration = new Pulumi.AzureNative.App.Inputs.LogAnalyticsConfigurationArgs
            {
                CustomerId = workspace.CustomerId,
                SharedKey = workspaceSharedKey
            }
        },
        WorkloadProfiles = new[]
        {
            new WorkloadProfileArgs
            {
                Name = "Consumption",
                WorkloadProfileType = "Consumption"
            }
        },
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Static Web App (Next.js Admin Dashboard) - declared before Container App for CORS reference
    var staticWebApp = new StaticSite($"swa-hotshot-{environment}", new StaticSiteArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new SkuDescriptionArgs
        {
            Name = "Free",
            Tier = "Free"
        },
        BuildProperties = new StaticSiteBuildPropertiesArgs
        {
            AppLocation = "1-Presentation/admin-dashboard",
            OutputLocation = "out",
            AppBuildCommand = "npm run build"
        },
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Container App (API) with Managed Identity authentication
    var containerApp = new ContainerApp($"ca-hotshot-api-{environment}", new ContainerAppArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        ManagedEnvironmentId = managedEnvironment.Id,
        // SECURITY: Attach managed identity to container app for secure registry access
        Identity = new Pulumi.AzureNative.App.Inputs.ManagedServiceIdentityArgs
        {
            Type = "UserAssigned",
            UserAssignedIdentities = new InputList<string>
            {
                containerAppIdentity.Id
            }
        },
        Configuration = new ConfigurationArgs
        {
            Ingress = new IngressArgs
            {
                External = true,
                TargetPort = 8080,
                Transport = "auto",
                Traffic = new[]
                {
                    new TrafficWeightArgs
                    {
                        LatestRevision = true,
                        Weight = 100
                    }
                },
                AllowInsecure = false
            },
            // SECURITY: Registry credentials now obtained from Key Vault via managed identity
            // No longer storing plaintext credentials in Pulumi state
            Registries = new[]
            {
                new RegistryCredentialsArgs
                {
                    Server = registry.LoginServer,
                    Identity = containerAppIdentity.Id
                    // Removed Username and PasswordSecretRef - using managed identity instead
                }
            },
            Secrets = new[]
            {
                new Pulumi.AzureNative.App.Inputs.SecretArgs
                {
                    Name = "db-connection-string",
                    Value = connectionString
                },
                new Pulumi.AzureNative.App.Inputs.SecretArgs
                {
                    Name = "appinsights-connection-string",
                    Value = appInsightsConnectionString
                }
                // Removed registry-password secret - no longer needed with managed identity
            }
        },
        Template = new TemplateArgs
        {
            Containers = new[]
            {
                new ContainerArgs
                {
                    Name = "hotshot-api",
                    // Dynamically construct the container image path:
                    // - If usePlaceholderImage=true: use Microsoft's hello-world image for initial deployment
                    // - Otherwise: use <registry>.azurecr.io/<imageName>:<imageTag>
                    Image = usePlaceholderImage
                        ? "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
                        : registry.LoginServer.Apply(server => $"{server}/{imageName}:{imageTag}"),
                    Resources = new ContainerResourcesArgs
                    {
                        Cpu = 0.5,
                        Memory = "1Gi"
                    },
                    Env = new[]
                    {
                        new EnvironmentVarArgs
                        {
                            Name = "ASPNETCORE_ENVIRONMENT",
                            Value = "Production"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "ASPNETCORE_URLS",
                            Value = "http://+:8080"
                        },
                        // Azure App Configuration endpoint - app loads config from here
                        new EnvironmentVarArgs
                        {
                            Name = "AppConfiguration__Endpoint",
                            Value = appConfig.Endpoint
                        },
                        // Key Vault URI - for secrets referenced from App Configuration
                        new EnvironmentVarArgs
                        {
                            Name = "KeyVault__VaultUri",
                            Value = keyVault.Properties.Apply(p => p.VaultUri)
                        },
                        // Managed Identity Client ID - for authenticating to App Config and Key Vault
                        new EnvironmentVarArgs
                        {
                            Name = "Azure__ManagedIdentityClientId",
                            Value = containerAppIdentity.ClientId
                        },
                        // Bootstrap secrets (until migrated to Key Vault)
                        new EnvironmentVarArgs
                        {
                            Name = "ConnectionStrings__DefaultConnection",
                            SecretRef = "db-connection-string"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                            SecretRef = "appinsights-connection-string"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "AllowedHosts",
                            Value = "*"
                        },
                        // CORS - computed from Static Web App hostname
                        new EnvironmentVarArgs
                        {
                            Name = "Cors__AllowedOrigins",
                            Value = staticWebApp.DefaultHostname.Apply(swaHost =>
                                $"https://{swaHost},http://localhost:3000")
                        }
                    }
                }
            },
            Scale = new ScaleArgs
            {
                MinReplicas = 0,
                MaxReplicas = 3,
                Rules = new[]
                {
                    new ScaleRuleArgs
                    {
                        Name = "http-scaling",
                        Http = new HttpScaleRuleArgs
                        {
                            Metadata = new InputMap<string>
                            {
                                { "concurrentRequests", "10" }
                            }
                        }
                    }
                }
            }
        },
        WorkloadProfileName = "Consumption",
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    }, new CustomResourceOptions
    {
        // Ensure role assignments are complete before creating the Container App
        DependsOn = { acrPullRoleAssignment, kvSecretsRoleAssignment, appConfigRoleAssignment }
    });

    // Export outputs
    // These outputs can be consumed by CI/CD pipelines via: pulumi stack output <name>
    return new Dictionary<string, object?>
    {
        // Resource Group
        ["resourceGroupName"] = resourceGroup.Name,
        
        // Container Registry (ACR)
        ["containerRegistryName"] = registry.Name,
        ["containerRegistryLoginServer"] = registry.LoginServer,
        
        // Container App
        ["containerAppName"] = containerApp.Name,
        ["containerAppUrl"] = containerApp.Configuration.Apply(c => c!.Ingress!.Fqdn),
        ["containerAppFullUrl"] = containerApp.Configuration.Apply(c => $"https://{c!.Ingress!.Fqdn}"),
        
        // Container Image (for CI/CD to know what to push)
        ["containerImageName"] = imageName,
        ["containerImageTag"] = imageTag,
        ["containerImageFullPath"] = registry.LoginServer.Apply(server => $"{server}/{imageName}:{imageTag}"),
        
        // Managed Identity
        ["containerAppIdentityId"] = containerAppIdentity.Id,
        ["containerAppIdentityClientId"] = containerAppIdentity.ClientId,
        ["containerAppPrincipalId"] = containerAppIdentity.PrincipalId,
        
        // Key Vault
        ["keyVaultName"] = keyVault.Name,
        ["keyVaultUri"] = keyVault.Properties.Apply(p => p.VaultUri),
        
        // App Configuration
        ["appConfigName"] = appConfig.Name,
        ["appConfigEndpoint"] = appConfig.Endpoint,
        
        // Static Web App
        ["staticWebAppUrl"] = staticWebApp.DefaultHostname,
        ["staticWebAppFullUrl"] = staticWebApp.DefaultHostname.Apply(h => $"https://{h}"),
        ["staticWebAppDeploymentToken"] = Output.CreateSecret(
            staticWebApp.Id.Apply(_ => "")
        ),
        
        // Application Insights (secrets)
        ["appInsightsInstrumentationKey"] = Output.CreateSecret(appInsightsInstrumentationKey),
        ["appInsightsConnectionString"] = Output.CreateSecret(appInsightsConnectionString),
        
        // SQL Server
        ["sqlServerFqdn"] = sqlServer.FullyQualifiedDomainName,
        ["databaseName"] = database.Name,
        
        // Log Analytics
        ["logAnalyticsWorkspaceId"] = workspace.CustomerId
    };
});
