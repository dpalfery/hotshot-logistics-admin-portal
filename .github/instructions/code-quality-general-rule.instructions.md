# Code Quality General Rule

## Build Quality (Zero Tolerance)
- Fix all build errors and warnings immediately
- Treat warnings as errors in CI/CD
- Never hardcode mock testing data in production
- One object/class/interface/enum per file

## Validation & Error Handling
- Validate all user inputs on client and server
- Sanitize and normalize inputs before use
- Return consistent error responses across APIs
- Handle exceptions gracefully without exposing sensitive details

## Observability
- Use structured logging with semantic properties
- Configure logging providers per environment
- Implement metrics collection for key KPIs
- Implement distributed tracing for request flows
- Configure health checks during application startup

## Testing Standards & Coverage
- **Application/Domain layers**: Minimum 80% coverage
- **Infrastructure/Persistence layer**: Minimum 70% coverage
- **Presentation layer**: Minimum 60% coverage
- **Frontend components**: Minimum 70% coverage
- **Mobile app**: Minimum 65% coverage

## Test Organization
- Place tests in appropriate directories following project structure
- Use descriptive test class and method names
- Group related tests in classes
- Separate unit, integration, and performance tests

## Code Review Quality Gates
- Ensure builds pass without warnings
- Ensure all tests pass and coverage meets requirements
- Ensure static analysis tools pass
- All new features require corresponding tests

## When to Apply
Apply to all development activities, regardless of technology stack or architectural layer.