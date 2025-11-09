# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Hotshot Logistics** is a full-stack logistics platform for hotshot delivery management with:
- **Admin Dashboard:** Next.js 15 (React 19) with TypeScript and Tailwind CSS v4
- **Driver Mobile App:** Expo (React Native 0.79+) for cross-platform delivery management
- **Backend API:** .NET 8 ASP.NET Core Web API with native ADO.NET data access
- **Database:** SQL Server with FluentMigrator for schema management

## Architecture

The codebase follows **Clean Architecture** principles with strict layering:

### Layer Structure

```
0-Base/          → Core types shared across all layers
1-Presentation/  → API controllers, admin dashboard, mobile app
2-Application/   → Business logic orchestration (services, validators)
3-Domain/        → Domain models and contracts (interfaces)
4-Persistence/   → Database repositories, migrations, data services
5-Test/          → xUnit tests (unit + integration)
6-Docs/          → All Documentation and markdown files except readmes but they should be short and point to detailed docs in 6-Docs/ folder. Create subfolders by topic
7-Deployment/    → Docker Compose, database setup scripts, IaC
```

### Dependency Flow

- **Presentation** depends on Application, Domain, Core
- **Application** depends on Domain, Core
- **Persistence** depends on Application, Domain, Core
- **Domain** depends only on Core
- **Core** has no dependencies

### Key Components

**Backend Services** (`2-Application/HotshotLogistics.Application/Services/`):
- `JobService` - Job creation, updates, status management
- `DriverService` - Driver management and availability
- `CustomerService` - Customer account management
- `BillingService` - Invoicing and payment processing
- `TrackingService` - Real-time location tracking
- `NotificationService` - Push notifications and alerts
- `RealtimeService` - SignalR hub management

**Repositories** (`4-Persistence/HotshotLogistics.Data/Repositories/`):
- All repositories use ADO.NET (`Microsoft.Data.SqlClient`) directly
- No ORM - raw SQL queries and stored procedures
- Repository pattern abstracts data access from business logic

**Domain Contracts** (`3-Domain/HotshotLogistics.Contracts/`):
- Interface definitions for repositories and services
- DTOs and domain models in `3-Domain/HotshotLogistics.Domain/`

## Common Development Commands

### .NET Backend

```bash
# Build the entire solution
dotnet build

# Run the API (from 1-Presentation/HotshotLogistics.Api/)
cd 1-Presentation/HotshotLogistics.Api
dotnet run
# API runs on https://localhost:7060 by default

# Run all tests
dotnet test

# Run specific test project
cd 5-Test/HotshotLogistics.Tests
dotnet test

# Run tests with filter
dotnet test --filter "FullyQualifiedName~JobService"

# Run integration tests
cd 5-Test/HotshotLogistics.IntegrationTests
dotnet test
```

### Database Migrations

**IMPORTANT:** Migrations use FluentMigrator and require `ConnectionStrings__DefaultConnection` environment variable (standard .NET configuration).

```bash
# Start local SQL Server (from 7-Deployment/)
cd 7-Deployment
docker-compose up -d

# Run DbSetup CLI to provision database and run migrations
# (from 7-Deployment/DbSetup/HotshotLogistics.DbSetup/)
export ConnectionStrings__DefaultConnection="Server=localhost;Database=hotshot_logistics;User Id=sa;Password=<PASSWORD>;TrustServerCertificate=true;"
dotnet run --project-slug "hotshot" \
  --server "localhost" \
  --db-name "hotshot_logistics" \
  --app-user "hotshot_app" \
  --app-password "<PASSWORD>"

# For CI/CD environments, set variables at process level (GitHub Actions, etc)
dotnet run --project-slug "hotshot" \
  --server "localhost" \
  --db-name "hotshot_logistics" \
  --app-user "hotshot_app" \
  --app-password "<PASSWORD>" \
  --env-vars-in-proc
```

**Environment Variable Configuration:**
- Standard .NET convention: `ConnectionStrings__DefaultConnection`
- Also supports case-insensitive variant for Linux: `CONNECTIONSTRINGS__DEFAULTCONNECTION`
- See `.env.example` for required environment variables and their format

Migration files are in `4-Persistence/HotshotLogistics.Data/Migrations/`.
FluentMigrator automatically tracks applied migrations in the `VersionInfo` table.

### Admin Dashboard (Next.js)

```bash
cd 1-Presentation/admin-dashboard

# Install dependencies
npm install

# Run dev server (with HTTPS)
npm run dev
# Runs at https://localhost:3001

# Build for production
npm run build

# Lint code
npm run lint

# Run Playwright tests
npm test

# Run Playwright in UI mode
npm run test:ui
```

### Driver Mobile App (Expo)

```bash
cd 1-Presentation/HotshotLogistics.mobile

npm install
npm run dev
# Or: npx expo start
```

## Configuration & Environment

### Required Environment Variables

Create a `.env` file from `.env.example`. Use standard .NET environment variable naming conventions:

```bash
# REQUIRED for database access (standard .NET configuration)
ConnectionStrings__DefaultConnection=Server=<SERVER>;Database=<DATABASE>;User Id=<USER>;Password=<PASSWORD>;TrustServerCertificate=true;

# REQUIRED for local SQL Server Docker setup
SQL_SA_PASSWORD=<PASSWORD>

# Optional for Azure integration
AZURE_CLIENT_ID=<CLIENT_ID>
AZURE_TENANT_ID=<TENANT_ID>
```

**Key Points:**
- Use `ConnectionStrings__DefaultConnection` (standard .NET convention with double underscores)
- Supports case-insensitive variant `CONNECTIONSTRINGS__DEFAULTCONNECTION` for Linux compatibility
- DbSetup CLI automatically sets these variables when running migrations
- For CI/CD, use `--env-vars-in-proc` flag to set at process level
- Replace `<PASSWORD>`, `<SERVER>`, `<DATABASE>`, etc. with actual values

**Security:** Never commit secrets. Use `.env.example` as a template and populate with actual values locally. Use Azure Key Vault for production.

### Configuration Files

- **API:** `1-Presentation/HotshotLogistics.Api/appsettings.json`
- **Dashboard:** `1-Presentation/admin-dashboard/.env.local` (generated from scripts/generate-env.js)
- **Database:** Connection strings via environment variables or Azure App Configuration

## Testing Strategy

### Test Projects

- **HotshotLogistics.Tests** - Unit tests for services, repositories, utilities
- **HotshotLogistics.IntegrationTests** - Integration tests for controllers and database
- **admin-dashboard** - Playwright E2E tests for frontend

### Running Specific Tests

```bash
# Single test class
dotnet test --filter "FullyQualifiedName~JobServiceTests"

# Single test method
dotnet test --filter "FullyQualifiedName~JobServiceTests.CreateJob_ValidInput_ReturnsJob"

# All tests in a namespace
dotnet test --filter "FullyQualifiedName~HotshotLogistics.Tests.Job"
```

### Integration Test Requirements

Integration tests require:
1. SQL Server running (via Docker Compose or local instance)
2. `TEST_DB_PASSWORD` environment variable set
3. Test database created (auto-created by test fixtures)

## Data Access Patterns

### ADO.NET with Microsoft.Data.SqlClient

The project uses **native ADO.NET** instead of Entity Framework:

- Direct SQL queries via `SqlCommand`
- Manual parameter binding for security (prevents SQL injection)
- Manual object mapping in repositories
- Connection string management via `ServiceCollectionExtensions`

### Example Repository Pattern

Repositories implement interfaces from `3-Domain/HotshotLogistics.Contracts/` and use raw SQL:

```csharp
// Example: 4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs
using var connection = new SqlConnection(_connectionString);
using var command = new SqlCommand("SELECT * FROM Jobs WHERE Id = @Id", connection);
command.Parameters.AddWithValue("@Id", jobId);
// ... execute and map results
```

## Frontend Architecture

### Admin Dashboard Tech Stack

- **Framework:** Next.js 15 (App Router)
- **State:** React Query (@tanstack/react-query) for server state
- **Tables:** @tanstack/react-table
- **Charts:** Recharts
- **Auth:** @azure/msal-react (Azure AD B2C)
- **Real-time:** @microsoft/signalr for live updates
- **Styling:** Tailwind CSS v4 with Headless UI and Radix UI components
- **Icons:** Lucide React

### Mobile App Tech Stack

- **Framework:** Expo (React Native)
- **Navigation:** Expo Router
- **Icons:** Lucide React Native
- **Maps:** React Native Maps

## Authentication & Authorization

- **Azure AD B2C** for admin dashboard authentication
- **Microsoft Identity Web** for API authentication
- JWT token validation with Azure AD issuer
- Configuration in `appsettings.json` under `AzureAdB2C`

## Real-time Features

SignalR hubs for:
- Job status updates
- Driver location tracking
- Live notifications

Hub configuration in `RealtimeService` and Azure SignalR Service integration.

## Known Issues & Workarounds

### Migration Skip Logic

The MigrationRunner has logic to skip `SeedContactsData` migration due to missing customer reference (`cust-003`). When adding new migrations, be aware of this skip logic in `Program.cs`.


## Code Quality

- **StyleCop Analyzers** enabled for HotshotLogistics.Data
- **ESLint** configured for Next.js dashboard
- **Nullable reference types** enabled across all .NET projects
- **InternalsVisibleTo** configured for test assemblies

## Git Workflow

- **Main branch:** `maint`
- **Current branch:** `development` (example feature branch)
- Create PRs against development branch for all changes
- Run tests before committing: `dotnet test` and `npm test`

## Additional Resources

- README.md - Detailed project setup and prerequisites
- SECURITY.md - Security policies and guidelines
- .editorconfig - Code formatting standards

## Pulumi for IaC
- All IaC should be written with Pulumi
- C# should be the script language for Pulumi
- Azure is our chosen cloud
