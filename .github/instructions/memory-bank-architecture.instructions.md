# Memory Bank Architecture

## System Architecture Overview
Clean Architecture with numbered folder structure (0-Base → 7-Deployment) enforcing separation of concerns and dependency direction.

## Key Technical Decisions
- **Native ADO.NET**: Direct SQL control for performance and flexibility
- **ASP.NET Core Web API**: Scalable RESTful APIs
- **Azure Functions**: Event-driven compute for async processing
- **SignalR**: Real-time WebSocket communication
- **FluentMigrator**: Database schema versioning
- **Repository Pattern**: Data access abstraction
- **CQRS Pattern**: Read/write separation
- **Azure Cloud Native**: Scalability and reliability

## Component Architecture

### Core Services API (.NET 8)
- RESTful endpoints for CRUD operations
- SignalR hubs for real-time updates
- Azure Functions for background processing
- External service integrations

### Admin Dashboard BFF (.NET 8)
- Data aggregation for dashboard needs
- Optimized endpoints for UI consumption
- SignalR connections for live updates
- Azure AD authentication

### Mobile App BFF (.NET 8)
- Mobile-optimized APIs
- Offline sync management
- Push notifications
- GPS data processing

### Admin Dashboard (Next.js/React)
- Component-based UI with atomic design
- React Query for server state
- Azure AD integration
- SignalR client for real-time updates

### Mobile App (React Native/Expo)
- Cross-platform iOS/Android
- Offline support with local SQLite
- Background GPS tracking
- Push notifications via Azure Notification Hubs

## Data Architecture
- **Primary Database**: SQL Server with optimized schema
- **Caching**: Redis for session and location data
- **File Storage**: Azure Blob Storage
- **Message Queue**: Azure Service Bus

## Key Domain Models
**Job Management**: Pickup/delivery locations, cargo details, pricing, real-time tracking
**Driver Management**: Profiles, certifications, performance metrics, earnings
**Customer Management**: Profiles, credit terms, billing preferences
**Financial Management**: Automated invoicing, payment processing, reporting

## Integration Architecture
**External Services**: Stripe/PayPal payments, Azure/Google Maps, Twilio/SendGrid communication, Azure AD authentication
**API Design**: RESTful endpoints with URL versioning, JWT tokens, OpenAPI documentation

## Performance Architecture
**Scalability**: Azure Container Apps consumption plan, horizontal scaling, database read replicas
**Real-time**: SignalR bidirectional communication, Azure Service Bus events, Redis caching
**CDN**: Azure Front Door for global asset delivery

## Deployment Architecture
**Infrastructure as Code**: Azure resources via Terraform/Pulumi
**CI/CD**: GitHub Actions multi-stage pipelines
**Security**: Key Vault, managed identities, RBAC
**Monitoring**: Application Insights, health checks

## When to Apply
Apply when designing system architecture, making technical decisions, or understanding component relationships.