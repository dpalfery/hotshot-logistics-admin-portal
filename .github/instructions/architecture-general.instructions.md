# Clean Architecture + DDD Folder Structure (C#)

## Layer Structure (0-7)
- **0-Base**: Core enums, exceptions, base repository interfaces
- **1-Presentation**: API controllers, SignalR hubs, middleware, DTOs
- **2-Application**: Services, validators, authorization, CQRS handlers
- **3-Domain**: Entities, value objects, domain services, contracts
- **4-Persistence**: DbContext, configurations, repositories, migrations
- **5-Tests**: Unit and integration tests
- **6-Docs**: Documentation, architectural decisions
- **7-Deployment**: Infrastructure as code, deployment scripts

## Key Principles
- Dependencies flow downward only (Domain → Application → Infrastructure)
- Domain is persistence-agnostic
- Repository pattern for data access
- CQRS for complex business logic
- Native ADO.NET over ORM for performance
- FluentMigrator for schema management

## Project Naming Convention
- `HotshotLogistics.{Layer}` (e.g., `HotshotLogistics.Domain`, `HotshotLogistics.Application`)
- Contracts in separate `HotshotLogistics.Contracts` project

## When to Apply
Apply when creating, moving, adding, or searching files to maintain separation of concerns and dependency direction across all technology stacks.