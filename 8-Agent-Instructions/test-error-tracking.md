# Test and Build Error Tracking

This file tracks build errors and failed tests identified during the test run on December 20, 2025.

## Summary of Runs

| Project | Type | Status | Details |
|---------|------|--------|---------|
| HotshotLogistics.sln | Build | Success | C# solution built successfully. |
| HotshotLogistics.Tests | Unit Test | Success | 214 tests passed. |
| HotshotLogistics.IntegrationTests | Integration Test | Failed | 85 tests failed. |
| admin-dashboard | Build | Failed | TypeScript compilation error. |
| admin-dashboard | Unit Test (Jest) | Failed | 41 tests failed. |
| HotshotLogistics.Playwright-UI.Tests | E2E Test | Failed | Authentication timeout. |

## Detailed Errors

### Build Errors

#### admin-dashboard
- **Type**: Build error
- **Source Project**: `admin-dashboard`
- **Summary**: `Type error: Block-scoped variable 'isLoading' used before its declaration.`
- **Location**: `src/components/auth/auth-provider.tsx:40:7`
- **Description**: The variable `isLoading` is used in a `useEffect` dependency array before it is declared in the component scope.

### Failed Tests

#### HotshotLogistics.IntegrationTests
- **Type**: Failed test
- **Source Project**: `HotshotLogistics.IntegrationTests`
- **Summary**: Database connection timeout (`SqlException`).
- **Count**: 85 failures.
- **Description**: All integration tests failed because the `DatabaseTestFixture` could not establish a connection to the SQL Server. Error: `A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible.`

#### HotshotLogistics.Playwright-UI.Tests
- **Type**: Failed test
- **Source Project**: `HotshotLogistics.Playwright-UI.Tests`
- **Summary**: Authentication setup timeout.
- **Description**: The global setup failed while waiting for the redirect to `login.microsoftonline.com`. Error: `TimeoutError: page.waitForURL: Timeout 60000ms exceeded.`

#### admin-dashboard (Jest)
- **Type**: Failed test
- **Source Project**: `admin-dashboard`
- **Summary**: Multiple failures (41 tests).
- **Key Issues**:
    - `ReferenceError: Cannot access 'isLoading' before initialization` in `AuthProvider`.
    - `TestingLibraryElementError: Unable to find an element with the text: Loading...`.
    - `Authentication readiness timeout` in `ApiService` tests.
    - `expect(jest.fn()).not.toHaveBeenCalled()` failures in `AuthContext` tests.
