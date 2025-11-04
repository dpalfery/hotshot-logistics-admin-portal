# Memory Bank Tech

## Technologies and Dependencies

### Custom KiloCode Modes
- **.Net Dev**: Use for all backend work (.NET 8, ASP.NET Core, Azure Functions)
- **Next.js Developer**: Use for all frontend work (Next.js, React, TypeScript)

### Backend (.NET)
- **Framework**: .NET 8, ASP.NET Core Web API with RESTful APIs
- **Database**: SQL Server with native ADO.NET (Microsoft.Data.SqlClient) and FluentMigrator
- **Authentication**: Microsoft.Identity.Web (Azure AD integration)
- **Real-time**: ASP.NET Core SignalR for WebSocket communication
- **Validation**: FluentValidation for business rule validation
- **Monitoring**: Application Insights for telemetry and diagnostics
- **HTTP Client**: IHttpClientFactory with Polly for resilience

### Frontend (Admin Dashboard)
- **Framework**: Next.js 14+ with App Router (React 18)
- **Language**: TypeScript with strict configuration
- **Styling**: TailwindCSS with custom design system
- **Charts**: Recharts for data visualization
- **Tables**: TanStack Table (React Table v8) for advanced data grids
- **State Management**: TanStack Query (React Query) for server state
- **Validation**: Zod for schema validation and type inference
- **UI Components**: Headless UI, Radix UI, custom component library
- **Icons**: Heroicons, Lucide React
- **Authentication**: MSAL for Azure AD integration
- **Real-time**: SignalR client for live updates

### Mobile (Driver App)
- **Framework**: Expo React Native (SDK 50+)
- **Language**: TypeScript with strict configuration
- **Navigation**: Expo Router for file-based routing
- **State Management**: AsyncStorage for local storage, React Query for API state
- **Location Services**: Expo Location with background tasks
- **Camera**: Expo Camera for proof of delivery
- **Maps**: React Native Maps with custom markers and routing
- **Push Notifications**: Expo Notifications with Azure Notification Hubs
- **Authentication**: MSAL React Native for Azure AD
- **Offline Support**: NetInfo and local SQLite database

### Infrastructure
- **Cloud**: Azure (Web Sites, SQL Database, Blob Storage, Redis Cache, Functions)
- **Containerization**: Docker, Docker Compose for local SQL server
- **CI/CD**: GitHub Actions with multi-stage pipelines
- **Infrastructure as Code**: TerraPulumi for resource provisioning
- **Message Queue**: Azure Service Bus for decoupled processing

### External Service Integrations
- **Payment Processing**: Stripe SDK, PayPal SDK
- **Mapping & Routing**: Azure Maps, Google Maps for geocoding and directions
- **Communication**: Twilio SDK for SMS, SendGrid for email
- **Geolocation**: GPS tracking with background location updates
- **File Storage**: Azure Blob Storage for document management
- **Push Notifications**: Azure Notification Hubs for cross-platform messaging

### Key Dependencies and Patterns

#### Core Dependencies
- **Microsoft.Data.SqlClient**: Native ADO.NET for high-performance database access
- **FluentMigrator**: Database schema versioning and migrations
- **ASP.NET Core Web API**: Scalable, full-featured framework for hosting RESTful APIs
- **Azure Functions**: Event-driven compute for processing messages, queues
- **SignalR**: Real-time bidirectional communication
- **React**: Component-based UI development
- **TypeScript**: Type-safe JavaScript development
- **Expo**: Cross-platform mobile development framework

#### Architectural Patterns
- **Clean Architecture**: Layered architecture with dependency inversion
- **Repository Pattern**: Abstraction over data access technologies
- **CQRS**: Command/query separation for complex business logic
- **Dependency Injection**: IoC container for loose coupling
- **Factory Pattern**: Service creation and external provider abstraction
- **Observer Pattern**: Real-time event notification and updates

#### Development Patterns
- **Async/Await**: Asynchronous programming throughout the stack
- **Result Pattern**: Error handling without exceptions
- **Builder Pattern**: Complex object construction
- **Strategy Pattern**: Algorithm selection and external service abstraction
- **Decorator Pattern**: Cross-cutting concerns like logging and caching

### Development Environment Setup

#### Prerequisites
- **.NET 8 SDK**: For backend development and build tools
- **Node.js 18+**: For frontend and mobile development
- **SQL Server**: Local database instance for development
- **Azure CLI**: For cloud resource management
- **Expo CLI**: For React Native development
- **Visual Studio 2022** or **VS Code**: Primary development IDE
- **Pulumi**: For IaC

#### Local Development Workflow
1. **Backend Setup**: Restore NuGet packages, run database migrations
2. **Frontend Setup**: Install npm dependencies, start development server
3. **Mobile Setup**: Install Expo dependencies, start Metro bundler
4. **Database**: Run FluentMigrator to create local database schema
5. **Azure Services**: Configure local Azure services for development

## When to Apply
Apply when setting up development environment, choosing technologies, or understanding project dependencies and patterns.