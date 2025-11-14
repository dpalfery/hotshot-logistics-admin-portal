# API Documentation (Swagger/OpenAPI)

This document explains how to generate and use the Swagger/OpenAPI documentation for the Hotshot Logistics API.

## Quick Start - Export Swagger JSON

The Swagger JSON file can be shared with mobile developers to generate client SDKs and understand the API structure.

### Option 1: Using the Export Script (Recommended)

**Windows (PowerShell):**
```powershell
cd 1-Presentation/HotshotLogistics.Api
.\export-swagger.ps1
```

**Mac/Linux (Bash):**
```bash
cd 1-Presentation/HotshotLogistics.Api
chmod +x export-swagger.sh
./export-swagger.sh
```

This will:
1. Start the API locally
2. Download the `swagger.json` file
3. Save it to `1-Presentation/HotshotLogistics.Api/swagger.json`
4. Stop the API

You can then share `swagger.json` with mobile developers!

### Option 2: Manual Export

1. Start the API:
   ```bash
   cd 1-Presentation/HotshotLogistics.Api
   dotnet run
   ```

2. Download the swagger.json:
   - Navigate to: https://localhost:7060/swagger/v1/swagger.json
   - Save the file

3. Stop the API (Ctrl+C)

## Viewing the Interactive API Documentation

When the API is running in Development mode, you can view the interactive Swagger UI:

1. Start the API:
   ```bash
   cd 1-Presentation/HotshotLogistics.Api
   dotnet run
   ```

2. Open your browser to:
   - **Swagger UI:** https://localhost:7060/swagger
   - **Raw JSON:** https://localhost:7060/swagger/v1/swagger.json

## Using Swagger JSON for Mobile Development

Mobile developers can use the `swagger.json` file to:

### 1. Generate Type-Safe API Clients

**React Native (TypeScript):**
```bash
npm install -g @openapitools/openapi-generator-cli
openapi-generator-cli generate -i swagger.json -g typescript-axios -o src/api/generated
```

**Swift (iOS):**
```bash
openapi-generator-cli generate -i swagger.json -g swift5 -o GeneratedAPI
```

**Kotlin (Android):**
```bash
openapi-generator-cli generate -i swagger.json -g kotlin -o GeneratedAPI
```

### 2. Import into API Testing Tools

- **Postman:** Import → Link → Paste the swagger.json URL or upload the file
- **Insomnia:** Import/Export → Import Data → From File → Select swagger.json
- **Swagger Editor:** https://editor.swagger.io → File → Import File

## Authentication

The API uses JWT Bearer tokens for authentication (Azure AD B2C in production, Test auth in development).

**In Swagger UI:**
1. Click the "Authorize" button (top right)
2. Enter your JWT token in the format: `Bearer <your-token>`
3. Click "Authorize"

All subsequent requests will include the Authorization header.

## API Endpoints Overview

The API includes the following controllers:

- **Jobs** (`/api/jobs`) - Create, update, and manage delivery jobs
- **Drivers** (`/api/drivers`) - Driver management and availability
- **Customers** (`/api/customers`) - Customer account management
- **Billing** (`/api/billing`) - Invoicing and payment processing
- **Tracking** (`/api/tracking`) - Real-time location tracking
- **Job Assignments** (`/api/jobassignments`) - Assign drivers to jobs
- **User Profile** (`/api/userprofile`) - User profile management

## Configuration

Swagger configuration is in [Program.cs](Program.cs#L59-L105):

- **API Info:** Title, version, description, contact
- **Authentication:** JWT Bearer scheme
- **XML Comments:** Automatic documentation from code comments
- **Security Requirements:** All endpoints require authentication by default

## Troubleshooting

### Swagger.json returns 404

Make sure the API is running:
```bash
dotnet run
```

### Export script fails

Check that you have the required environment variables set (especially `DB_CONNECTION_STRING` if the API requires database access).

### Missing endpoint documentation

Add XML comments to your controller methods:
```csharp
/// <summary>
/// Gets a job by ID
/// </summary>
/// <param name="id">The job ID</param>
/// <returns>The job details</returns>
[HttpGet("{id}")]
public async Task<ActionResult<Job>> GetJob(string id)
{
    // ...
}
```

## Additional Resources

- [OpenAPI Specification](https://swagger.io/specification/)
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Generator](https://openapi-generator.tech/)
