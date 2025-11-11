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
7-Deployment/    → Docker Compose, database setup scripts
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

**IMPORTANT:** Migrations use FluentMigrator and require `DB_CONNECTION_STRING` environment variable.

```bash
# Run migrations (from 4-Persistence/MigrationRunner/)
cd 4-Persistence/MigrationRunner
DB_CONNECTION_STRING="Server=localhost;Database=HotshotDB;..." dotnet run

# Rerun all migrations from scratch
DB_CONNECTION_STRING="Server=localhost;Database=HotshotDB;..." dotnet run -- --rerun-all

# Start local SQL Server (from 7-Deployment/)
cd 7-Deployment
docker-compose up -d
```

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

# Run all tests (unit + component + E2E)
npm test

# Run unit & component tests (Vitest)
npm run test:unit
npm run test:unit:watch       # Watch mode for development
npm run test:unit:coverage    # With coverage report
npm run test:unit:ui          # Interactive UI

# Run E2E tests (Playwright)
npm run test:e2e
npm run test:e2e:headed       # With visible browser
npm run test:e2e:ui           # Interactive UI

# Run all tests in CI mode
npm run test:ci
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

Create a `.env` file from `.env.example`:

```bash
# REQUIRED for local development
SQL_SA_PASSWORD=<strong-password>
TEST_DB_PASSWORD=<strong-password>
DB_CONNECTION_STRING=Server=localhost;Database=HotshotDB;User Id=sa;Password=...;

# Optional for Azure integration
AZURE_CLIENT_ID=...
AZURE_TENANT_ID=...
CONNECTIONSTRINGS__DEFAULTCONNECTION=...
```

**Security:** Never commit secrets. Use Azure Key Vault for production.

### Configuration Files

- **API:** `1-Presentation/HotshotLogistics.Api/appsettings.json`
- **Dashboard:** `1-Presentation/admin-dashboard/.env.local` (generated from scripts/generate-env.js)
- **Database:** Connection strings via environment variables or Azure App Configuration

## Testing Strategy

### Backend Test Projects

- **HotshotLogistics.Tests** - Unit tests for services, repositories, utilities
- **HotshotLogistics.IntegrationTests** - Integration tests for controllers and database

### Frontend Testing (3-Tier Pyramid)

The admin dashboard follows a comprehensive testing pyramid:

```
         /\
        /E2E\      ← Playwright (full flows, critical paths)
       /------\
      /Component\ ← React Testing Library (UI components)
     /----------\
    /   Unit     \ ← Vitest (utilities, services, hooks)
   /--------------\
```

**Unit & Component Tests (Vitest + React Testing Library):**
- Location: `1-Presentation/admin-dashboard/src/**/*.test.ts(x)`
- Framework: Vitest with jsdom environment
- Coverage thresholds: 60% (lines, functions, branches, statements)
- Run: `npm run test:unit` or `npm run test:unit:coverage`

**E2E Tests (Playwright):**
- Location: `1-Presentation/admin-dashboard/tests/*.spec.ts`
- Browsers: Chromium, Firefox, WebKit
- Run: `npm run test:e2e`

**See:** `6-Docs/frontend-testing.md` for comprehensive frontend testing guide

### Running Backend Tests

```bash
# Single test class
dotnet test --filter "FullyQualifiedName~JobServiceTests"

# Single test method
dotnet test --filter "FullyQualifiedName~JobServiceTests.CreateJob_ValidInput_ReturnsJob"

# All tests in a namespace
dotnet test --filter "FullyQualifiedName~HotshotLogistics.Tests.Job"
```

### Running Frontend Tests

```bash
# Unit & component tests
cd 1-Presentation/admin-dashboard
npm run test:unit              # Run once
npm run test:unit:watch        # Watch mode
npm run test:unit:coverage     # With coverage

# E2E tests
npm run test:e2e               # Run Playwright tests
npm run test:e2e:ui            # Interactive mode

# All tests
npm test                       # Run all tests
npm run test:ci                # CI mode with coverage
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

## Build Pipeline

### Backend CI/CD (`.github/workflows/dotnet-build-test.yml`)

- **Restore** - All projects including Pulumi infrastructure
- **Lint** - Code formatting validation for all projects
- **Build** - Release build of all projects
- **Test** - Unit tests (excludes integration tests which require SQL Server)
- **Secret Scanning** - TruffleHog verification on all changes
- **Dependabot** - Automated dependency update checking

All projects including the Pulumi infrastructure (`7-Deployment/pulumi/`) are built and linted as part of the standard pipeline.

### Frontend CI/CD (`.github/workflows/frontend-tests.yml`)

Automated testing runs on every push/PR with parallel jobs:

1. **Unit & Component Tests** - Vitest with coverage reporting, uploads to Codecov
2. **E2E Tests** - Playwright tests with artifact uploads (reports, screenshots)
3. **Lint** - ESLint code style enforcement
4. **Build** - Production build verification

**Deployment Gate:** All tests must pass before deployment to Azure Static Web Apps (`.github/workflows/deploy-static-web-app.yml`)

## Known Issues & Workarounds

### Migration Skip Logic

The MigrationRunner has logic to skip `SeedContactsData` migration due to missing customer reference (`cust-003`). When adding new migrations, be aware of this skip logic in `Program.cs`.

### Test Compilation Errors

Some test files reference repositories directly (e.g., `DriverRepository`) which may need interface references instead. Check `5-Test/HotshotLogistics.Tests/Utils/Integration/` for affected tests.

## Code Quality

- **StyleCop Analyzers** enabled for HotshotLogistics.Data
- **ESLint** configured for Next.js dashboard
- **Nullable reference types** enabled across all .NET projects
- **InternalsVisibleTo** configured for test assemblies

## Git Workflow

- **Main branch:** `demo/dp-1-full-context`
- **Current branch:** `fix/dp-fix-all-warnings` (example feature branch)
- Create PRs against main branch for all changes
- Run tests before committing: `dotnet test` and `npm test`

## Additional Resources

- **README.md** - Detailed project setup and prerequisites
- **SECURITY.md** - Security policies and guidelines
- **6-Docs/frontend-testing.md** - Comprehensive frontend testing guide
- **.editorconfig** - Code formatting standards
