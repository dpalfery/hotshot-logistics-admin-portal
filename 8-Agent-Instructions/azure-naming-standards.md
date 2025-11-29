name: "Azure-Naming-Standards"
description: "Enforces the standard naming convention for all Azure resources in the Hotshot Logistics project."
when-to-apply: "Apply when creating, modifying, or reviewing Azure resources in Pulumi or Infrastructure-as-Code."
rule: |

# Azure Naming Standards

All Azure resources must follow the strict naming convention:

`{slug}-{environment}-{region}-{appName}`

## Definitions

- **Slug**: The standard Azure resource abbreviation (see table below).
- **Environment**: The target environment code (e.g., `dev`, `qa`, `prod`, `stg`).
- **Region**: The Azure region short code (e.g., `eastus`, `centralus`, `westeurope`).
- **AppName**: The application or project name (e.g., `hotshot`).

## Examples

| Resource Type | Slug | Pattern | Example |
|Data | --- | --- | --- |
| Resource Group | `rg` | `rg-{env}-{region}-{app}` | `rg-dev-eastus-hotshot` |
| Key Vault | `kv` | `kv-{env}-{region}-{app}` | `kv-dev-eastus-hotshot` |
| App Service Plan | `asp` | `asp-{env}-{region}-{app}` | `asp-dev-eastus-hotshot` |
| Container App | `ca` | `ca-{env}-{region}-{app}` | `ca-dev-eastus-hotshot` |
| SQL Server | `sql` | `sql-{env}-{region}-{app}` | `sql-dev-eastus-hotshot` |
| SQL Database | `sqldb` | `sqldb-{env}-{region}-{app}` | `sqldb-dev-eastus-hotshot` |
| Log Analytics | `log` | `log-{env}-{region}-{app}` | `log-dev-eastus-hotshot` |
| App Insights | `appi` | `appi-{env}-{region}-{app}` | `appi-dev-eastus-hotshot` |
| Identity | `id` | `id-{env}-{region}-{app}` | `id-dev-eastus-hotshot` |
| App Configuration | `appcs` | `appcs-{env}-{region}-{app}` | `appcs-dev-eastus-hotshot` |
| Static Web App | `swa` | `swa-{env}-{region}-{app}` | `swa-dev-eastus-hotshot` |

## Exceptions

Some Azure resources have strict naming requirements (length limits, no hyphens, alphanumeric only). For these, modify the pattern as follows:

| Resource Type | Slug | Pattern | Example | Note |
| --- | --- | --- | --- | --- |
| Storage Account | `st` | `st{env}{region}{app}` | `stdeveastushotshot` | Lowercase, no separators, max 24 chars. |
| Container Registry | `cr` | `cr{env}{region}{app}` | `crdeveastushotshot` | Alphanumeric, no separators. |

## Enforcement

- **Pulumi/Terraform**: Construct names dynamically using variables.
  ```csharp
  // Correct
  var name = $"rg-{environment}-{location}-{appName}";
  
  // Incorrect
  var name = $"rg-hotshot-{environment}";
  ```
- **Validation**: Ensure variable order is strictly `{slug}-{env}-{region}-{app}`.
