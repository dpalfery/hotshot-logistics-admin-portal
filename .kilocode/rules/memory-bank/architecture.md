# Hotshot Logistics Platform - Architecture

## System Architecture Overview

The Hotshot Logistics project follows Clean Architecture principles with a numbered folder structure to enforce separation of concerns and dependency direction. The architecture is designed for scalability, maintainability, and real-time operations in a cloud-native environment.

### Key Technical Decisions

1. **Native ADO.NET over ORM**: Direct SQL control for performance and flexibility in logistics operations
2. **ASP.NET Core Web API**: Scalable, full-featured framework for hosting RESTful APIs
3. **Azure Functions**: Event-driven compute for processing messages, queues, or other asynchronous triggers
4. **SignalR for Real-time**: WebSocket-based communication for live driver tracking and updates
5. **FluentMigrator**: Database versioning without Entity Framework dependencies
6. **Repository Pattern**: Abstraction over data access for flexibility across logistics workflows
7. **CQRS Pattern**: Separation of read and write operations for complex job management
8. **Azure Cloud Native**: Leveraging Azure services for scalability and reliability in logistics operations
9. **Cross-platform Mobile**: React Native for iOS and Android driver app coverage
10. **Backend for Frontend (BFF) Pattern**: Dedicated backend services for each frontend (Admin Dashboard, Mobile App) to optimize data transfer and tailor logic
11. **Database Migrations**: All database schema changes must be implemented as a new migration in the `4-Persistence/HotshotLogistics.Data` project using FluentMigrator. After creating the migration, the database must be updated by running the `MigrationRunner` project.

### Component Architecture

#### Core Services API (.NET 8)
- **HTTP APIs**: ASP.NET Core Web API RESTful endpoints for CRUD operations on jobs, drivers, customers
- **Real-time Communication**: SignalR hubs for live GPS tracking and dispatch updates
- **Background Processing**: Azure Functions for automated billing and notification scheduling
- **External Integrations**: Payment gateways, mapping services, SMS/email for logistics operations

#### Admin Dashboard BFF (.NET 8 ASP.NET Core Web API)
- **Data Aggregation**: Aggregates and transforms data from Core Services API for dashboard-specific needs
- **Optimized Endpoints**: Provides dashboard-optimized endpoints for job management, analytics, and reporting
- **Real-time Communication**: Manages SignalR connections for live dashboard updates
- **Authentication & Authorization**: Handles Azure AD authentication and role-based access for logistics managers

#### Mobile App BFF (.NET 8 ASP.NET Core Web API)
- **Mobile-Optimized APIs**: Tailors data and endpoints specifically for mobile app requirements
- **Offline Support**: Manages data synchronization and offline capabilities for drivers
- **Push Notifications**: Handles push notification delivery and management
- **Location Services**: Processes GPS data and location-based operations for real-time tracking

#### Admin Dashboard (Next.js/React)
- **Component Architecture**: Atomic design with reusable UI components for logistics workflows
- **State Management**: React Query for server state, Context API for local state
- **API Communication**: Communicates with Admin Dashboard BFF for optimized data retrieval and real-time updates
- **Authentication**: Azure AD integration with role-based access for logistics managers

#### Mobile App (React Native/Expo)
- **Cross-platform**: Single codebase for iOS and Android driver operations
- **Offline Support**: Local data storage and sync for remote delivery areas
- **API Communication**: Communicates with Mobile App BFF for tailored mobile-specific operations
- **Native Integration**: Camera for proof of delivery, GPS for real-time tracking

### Data Architecture

#### Database Design
- **Primary Database**: SQL Server with optimized schema for logistics operations (jobs, drivers, customers, tracking)
- **Caching Layer**: Redis for session management and real-time location data
- **File Storage**: Azure Blob Storage for delivery proofs and driver documents
- **Message Queue**: Azure Service Bus for decoupled processing of logistics events

#### Key Domain Models

**Job Management**
- Jobs with pickup/delivery locations, cargo details, pricing
- Real-time tracking with location updates and ETAs
- Status workflow: Pending → Assigned → In Progress → Completed

**Driver Management**
- Driver profiles with vehicle info, certifications, performance metrics
- Availability tracking and automated scheduling
- Earnings calculation and payment processing

**Customer Management**
- Customer profiles with credit terms and billing preferences
- Contract management and pricing agreements
- Communication preferences and history

**Financial Management**
- Automated invoice generation from completed jobs
- Payment processing with multiple gateways
- Accounts receivable tracking and reporting

### Integration Architecture

#### External Service Integrations
- **Payment Processing**: Stripe, PayPal for customer payments and driver earnings
- **Mapping Services**: Google Maps, Azure Maps for routing and geocoding in logistics
- **Communication**: Twilio SMS, SendGrid email, Azure Notification Hubs for delivery updates
- **Authentication**: Azure Active Directory for user management across platforms

#### API Design
- **RESTful Endpoints**: Resource-based URLs with proper HTTP methods for logistics operations
- **Versioning Strategy**: URL-based versioning (/api/v1/) for backward compatibility
- **Authentication**: JWT tokens with Azure AD integration
- **Documentation**: OpenAPI/Swagger with interactive testing for API consumers

### Performance Architecture

#### Scalability Patterns
- **Horizontal Scaling**: Azure Container Apps – Consumption Plan based on logistics operation load
- **Database Optimization**: Read replicas and query optimization for real-time tracking
- **Caching Strategy**: Multi-level caching with Redis for location data and job status
- **CDN Integration**: Azure Front Door for global content delivery of logistics assets

#### Real-time Architecture
- **WebSocket Communication**: SignalR for bidirectional communication in logistics operations
- **Event Streaming**: Azure Service Bus for event distribution of job updates
- **Live Updates**: Real-time dashboard updates and mobile notifications for delivery tracking
- **Location Tracking**: Efficient GPS data processing and storage for fleet management

### Deployment Architecture

#### Infrastructure as Code
- **Azure Resources**: Azure Container Apps – Consumption Plan, SQL Database, Storage, Redis Cache for logistics platform
- **Networking**: Virtual networks, security groups, private endpoints for data security
- **Monitoring**: Application Insights, Azure Monitor, health checks for operational visibility
- **Security**: Key Vault, managed identities, RBAC for logistics data protection

#### CI/CD Pipeline
- **Build Process**: Automated builds with dependency management for .NET, React, React Native
- **Testing**: Unit, integration, and performance tests for logistics workflows
- **Deployment**: Blue-green deployments with zero downtime for continuous operations
- **Rollback**: Automated rollback capabilities for logistics platform stability