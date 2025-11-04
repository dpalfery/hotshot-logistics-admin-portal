# Testing General Rule

## Test Coverage Standards
- **Application and Domain layers**: Minimum 80% code coverage
- **Infrastructure/Persistence layer**: Minimum 70% code coverage
- **Presentation layer**: Minimum 60% code coverage
- **Frontend components**: Minimum 70% coverage for critical user interactions
- **Mobile app**: Minimum 65% coverage for core functionality

## Test Organization
- Place tests in appropriate test directories following project structure
- Use descriptive test class and method names
- Group related tests in test classes
- Separate unit, integration, and performance tests

## Unit Testing
- All business logic must be covered by unit tests
- Mock dependencies appropriately
- Use readable assertions
- Test both happy path and error scenarios
- Follow AAA pattern (Arrange, Act, Assert)

## Integration Testing
- Test component interactions against real dependencies
- Use transactions to ensure test isolation
- Validate complete workflows and data flows
- Test error scenarios (timeouts, network failures)
- Validate data serialization and deserialization

## End-to-End Testing
- Test complete request-response cycles
- Validate HTTP status codes and response formats
- Test authentication middleware and security
- Include performance assertions in E2E tests

## Performance Testing
- Define realistic user load patterns and ramp-up strategies
- Monitor system metrics (CPU, memory, network)
- Test under various conditions (normal, peak, stress)
- Establish baseline performance metrics

## Test Execution and Automation
- Execute all tests on every pull request
- Run tests in parallel for faster feedback
- Generate test reports and coverage artifacts
- Block deployments if tests fail or coverage drops

## Quality Gates
- All new features require corresponding tests
- Review test quality and coverage in PRs
- Fail builds on test failures
- Enforce minimum coverage thresholds

## When to Apply
Apply when writing, executing, or reviewing tests for all components of the Hotshot Logistics platform.