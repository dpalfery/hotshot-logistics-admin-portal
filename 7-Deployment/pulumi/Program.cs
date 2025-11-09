using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Sql;
using System.Linq;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Pulumi.Config();
    var location = config.Get("location") ?? "eastus";
    var environment = config.Get("environment") ?? "dev";
    var sqlAdminPassword = config.RequireSecret("sqlAdminPassword");
    var containerImage = config.Get("containerImage") ?? "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest";

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
        Sku = new WorkspaceSkuArgs
        {
            Name = "PerGB2018"
        },
        RetentionInDays = 30,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Application Insights
    var appInsights = new Component($"appi-hotshot-{environment}", new ComponentArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Kind = "web",
        ApplicationType = "web",
        WorkspaceResourceId = workspace.Id,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    // Container Registry
    var registry = new Registry($"crhotshot{environment}", new RegistryArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new SkuArgs
        {
            Name = "Basic"
        },
        AdminUserEnabled = true,
        Tags = new InputMap<string>
        {
            { "Environment", environment },
            { "Project", "HotshotLogistics" }
        }
    });

    var registryCredentials = Output.Tuple(resourceGroup.Name, registry.Name).Apply(t =>
        ListRegistryCredentials.InvokeAsync(new ListRegistryCredentialsArgs
        {
            ResourceGroupName = t.Item1,
            RegistryName = t.Item2
        }));

    var registryUsername = registryCredentials.Apply(c => c.Username ?? "");
    var registryPassword = Output.CreateSecret(registryCredentials.Apply(c => c.Passwords.First().Value ?? ""));

    // SQL Server
    var sqlServer = new Server($"sql-hotshot-{environment}", new ServerArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        AdministratorLogin = "sqladmin",
        AdministratorLoginPassword = sqlAdminPassword,
        Version = "12.0",
        MinimalTlsVersion = "1.2",
        PublicNetworkAccess = ServerPublicNetworkAccess.Enabled,
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

    // Firewall rule to allow Azure services
    var firewallRule = new FirewallRule($"sqlfw-azure-services-{environment}", new FirewallRuleArgs
    {
        ResourceGroupName = resourceGroup.Name,
        ServerName = sqlServer.Name,
        StartIpAddress = "0.0.0.0",
        EndIpAddress = "0.0.0.0" // Special rule to allow Azure services
    });

    // Build connection string (marked as secret to prevent exposure in state)
    var connectionString = Output.CreateSecret(
        Output.Tuple(sqlServer.FullyQualifiedDomainName, database.Name, sqlAdminPassword)
            .Apply(t => $"Server=tcp:{t.Item1},1433;Initial Catalog={t.Item2};Persist Security Info=False;User ID=sqladmin;Password={t.Item3};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"));

    // Container Apps Environment (Consumption only - Workload Profiles v2)
    var managedEnvironment = new ManagedEnvironment($"cae-hotshot-{environment}", new ManagedEnvironmentArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        AppLogsConfiguration = new AppLogsConfigurationArgs
        {
            Destination = "log-analytics",
            LogAnalyticsConfiguration = new LogAnalyticsConfigurationArgs
            {
                CustomerId = workspace.CustomerId,
                SharedKey = workspace.GetKeys.Apply(keys => keys.PrimarySharedKey ?? "")
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

    // Container App (API)
    var containerApp = new ContainerApp($"ca-hotshot-api-{environment}", new ContainerAppArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        ManagedEnvironmentId = managedEnvironment.Id,
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
            Registries = new[]
            {
                new RegistryCredentialsArgs
                {
                    Server = registry.LoginServer,
                    Username = registryUsername,
                    PasswordSecretRef = "registry-password"
                }
            },
            Secrets = new[]
            {
                new SecretArgs
                {
                    Name = "registry-password",
                    Value = registryPassword
                },
                new SecretArgs
                {
                    Name = "db-connection-string",
                    Value = connectionString
                },
                new SecretArgs
                {
                    Name = "appinsights-connection-string",
                    Value = appInsights.ConnectionString
                }
            }
        },
        Template = new TemplateArgs
        {
            Containers = new[]
            {
                new ContainerArgs
                {
                    Name = "hotshot-api",
                    Image = Output.Format($"{registry.LoginServer}/{containerImage}"),
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
                        new EnvironmentVarArgs
                        {
                            Name = "ConnectionStrings__DefaultConnection",
                            SecretRef = "db-connection-string"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                            SecretRef = "appinsights-connection-string"
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
    });

    // Static Web App (Next.js Admin Dashboard)
    var staticWebApp = new StaticSite($"swa-hotshot-{environment}", new StaticSiteArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new Pulumi.AzureNative.Web.Inputs.SkuDescriptionArgs
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

    // Export outputs
    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        ["containerRegistryName"] = registry.Name,
        ["containerRegistryLoginServer"] = registry.LoginServer,
        ["containerAppUrl"] = containerApp.Configuration.Apply(c => c!.Ingress!.Fqdn),
        ["staticWebAppUrl"] = staticWebApp.DefaultHostname,
        ["staticWebAppDeploymentToken"] = staticWebApp.GetSecrets.Apply(s => s.Properties ?? ""),
        ["appInsightsInstrumentationKey"] = appInsights.InstrumentationKey,
        ["appInsightsConnectionString"] = appInsights.ConnectionString,
        ["sqlServerFqdn"] = sqlServer.FullyQualifiedDomainName,
        ["databaseName"] = database.Name,
        ["logAnalyticsWorkspaceId"] = workspace.CustomerId
    };
});
