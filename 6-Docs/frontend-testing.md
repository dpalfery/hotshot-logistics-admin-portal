# Frontend Testing Guide

This guide covers the comprehensive testing strategy for the Hotshot Logistics Admin Dashboard.

## Testing Pyramid

Our frontend follows a 3-tier testing pyramid strategy:

```
         /\
        /E2E\      ← Playwright (full flows, critical paths)
       /------\
      /Component\ ← React Testing Library (UI components)
     /----------\
    /   Unit     \ ← Vitest (utilities, services, hooks)
   /--------------\
```

## Test Types

### 1. Unit Tests (Vitest)

**Purpose:** Test individual functions, utilities, and services in isolation.

**Location:** `src/**/*.test.ts`

**Examples:**
- API service methods
- Utility functions
- Custom hooks
- Business logic

**Running Unit Tests:**

```bash
# Run all unit tests
npm run test:unit

# Run in watch mode (for development)
npm run test:unit:watch

# Run with coverage
npm run test:unit:coverage

# Run with interactive UI
npm run test:unit:ui
```

**Example Test Structure:**

```typescript
// src/services/api.test.ts
import { describe, it, expect, vi } from 'vitest';
import { apiService } from './api';

describe('ApiService', () => {
  it('should fetch jobs successfully', async () => {
    // Arrange
    const mockJobs = [{ id: '1', status: 'Pending' }];
    global.fetch = vi.fn().mockResolvedValueOnce({
      ok: true,
      json: async () => ({ items: mockJobs })
    });

    // Act
    const result = await apiService.getJobs();

    // Assert
    expect(result.items).toEqual(mockJobs);
  });
});
```

### 2. Component Tests (React Testing Library)

**Purpose:** Test React components, their rendering, user interactions, and state management.

**Location:** `src/components/**/*.test.tsx`

**Examples:**
- Component rendering
- User interactions (clicks, form inputs)
- Component state changes
- Props validation

**Running Component Tests:**

Component tests run with the same commands as unit tests (they're all run by Vitest):

```bash
npm run test:unit
npm run test:unit:watch
npm run test:unit:coverage
```

**Example Test Structure:**

```typescript
// src/components/dashboard/JobStatusCards.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import JobStatusCards from './JobStatusCards';

describe('JobStatusCards', () => {
  it('should render status cards with correct data', async () => {
    // Arrange
    const queryClient = new QueryClient();
    vi.mocked(apiService.getJobStatusSummary).mockResolvedValueOnce({
      pendingCount: 5,
      assignedCount: 10,
      enRouteCount: 3,
      receivedCount: 12,
    });

    // Act
    render(
      <QueryClientProvider client={queryClient}>
        <JobStatusCards />
      </QueryClientProvider>
    );

    // Assert
    await waitFor(() => {
      expect(screen.getByTestId('status-card-pending')).toHaveTextContent('5');
    });
  });
});
```

### 3. E2E Tests (Playwright)

**Purpose:** Test complete user flows and critical paths through the application.

**Location:** `tests/**/*.spec.ts`

**Examples:**
- User authentication flow
- Job creation workflow
- Driver assignment process
- Billing operations

**Running E2E Tests:**

```bash
# Run all E2E tests
npm run test:e2e

# Run in headed mode (see browser)
npm run test:e2e:headed

# Run with interactive UI
npm run test:e2e:ui
```

**Example Test Structure:**

```typescript
// tests/jobs-management.spec.ts
import { test, expect } from '@playwright/test';

test('should create a new job', async ({ page }) => {
  // Navigate
  await page.goto('/jobs');

  // Interact
  await page.click('button:has-text("Create Job")');
  await page.fill('[name="customerId"]', 'cust-001');
  await page.fill('[name="pickupLocation"]', '123 Main St');

  // Assert
  await expect(page.locator('.job-list')).toContainText('123 Main St');
});
```

## Running All Tests

```bash
# Run all tests (unit + component + E2E)
npm test

# Run all tests in CI mode
npm run test:ci
```

## Test Configuration

### Vitest Configuration

**File:** `vitest.config.ts`

Key features:
- **Environment:** jsdom (simulates browser environment)
- **Coverage provider:** v8
- **Coverage thresholds:** 60% for lines, functions, branches, statements
- **Setup file:** `src/test/setup.ts` (contains global test configuration)

### Playwright Configuration

**File:** `playwright.config.ts`

Key features:
- **Browsers:** Chromium, Firefox, WebKit
- **Retries:** 2 on CI, 0 locally
- **Base URL:** Configured for local development and CI
- **Reporters:** HTML, line, JSON

## Writing Tests

### Best Practices

1. **Follow the AAA Pattern:**
   - **Arrange:** Set up test data and mocks
   - **Act:** Execute the code being tested
   - **Assert:** Verify the results

2. **Test Behavior, Not Implementation:**
   ```typescript
   // ✅ Good - tests behavior
   expect(screen.getByRole('button', { name: /create job/i })).toBeInTheDocument();

   // ❌ Bad - tests implementation
   expect(component.state.isFormOpen).toBe(true);
   ```

3. **Use Descriptive Test Names:**
   ```typescript
   // ✅ Good
   it('should display error message when API call fails', () => {});

   // ❌ Bad
   it('test error', () => {});
   ```

4. **Mock External Dependencies:**
   ```typescript
   vi.mock('@/services/api', () => ({
     apiService: {
       getJobs: vi.fn(),
     },
   }));
   ```

5. **Clean Up After Tests:**
   ```typescript
   beforeEach(() => {
     vi.clearAllMocks();
   });

   afterEach(() => {
     vi.restoreAllMocks();
   });
   ```

### Testing Async Code

```typescript
// Use waitFor for async operations
await waitFor(() => {
  expect(screen.getByText('Data loaded')).toBeInTheDocument();
});

// Use findBy queries (automatically wait)
const element = await screen.findByText('Data loaded');
expect(element).toBeInTheDocument();
```

### Testing User Interactions

```typescript
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

it('should handle button click', async () => {
  const user = userEvent.setup();
  render(<MyComponent />);

  await user.click(screen.getByRole('button'));

  expect(mockHandler).toHaveBeenCalled();
});
```

## Code Coverage

### Coverage Reports

Coverage reports are generated automatically when running:

```bash
npm run test:unit:coverage
```

**Report Locations:**
- **HTML Report:** `coverage/index.html` (open in browser)
- **LCOV Report:** `coverage/lcov.info` (for CI tools)
- **Console Summary:** Displayed after test run

### Coverage Thresholds

Minimum coverage requirements (enforced in CI):
- **Lines:** 60%
- **Functions:** 60%
- **Branches:** 60%
- **Statements:** 60%

### Viewing Coverage

```bash
# Generate coverage and open HTML report
npm run test:unit:coverage
open coverage/index.html
```

## CI/CD Integration

### Automated Testing Pipeline

All tests run automatically on:
- **Push to main branch**
- **Pull request creation/updates**
- **Manual workflow dispatch**

**Pipeline stages:**

1. **Unit & Component Tests**
   - Runs Vitest tests
   - Generates coverage report
   - Uploads coverage to Codecov
   - Comments coverage on PRs

2. **E2E Tests**
   - Installs Playwright browsers
   - Runs E2E tests
   - Uploads test reports and screenshots

3. **Lint**
   - Runs ESLint
   - Enforces code style

4. **Build**
   - Builds production bundle
   - Verifies no build errors

5. **Deploy** (only if all tests pass)
   - Deploys to Azure Static Web Apps

### Test Artifacts

Test artifacts are automatically uploaded and retained for 7 days:
- **Coverage reports:** `coverage-report`
- **Playwright reports:** `playwright-report`
- **Test results:** `playwright-results`
- **Build artifacts:** `nextjs-build`

## Debugging Tests

### Debug Unit Tests

```bash
# Run specific test file
npm run test:unit src/services/api.test.ts

# Run specific test by name pattern
npm run test:unit -- -t "should fetch jobs"

# Run with UI for debugging
npm run test:unit:ui
```

### Debug E2E Tests

```bash
# Run in headed mode
npm run test:e2e:headed

# Run with UI mode
npm run test:e2e:ui

# Debug specific test
npx playwright test tests/jobs-management.spec.ts --debug
```

### VSCode Debugging

Add to `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "type": "node",
      "request": "launch",
      "name": "Debug Vitest Tests",
      "runtimeExecutable": "npm",
      "runtimeArgs": ["run", "test:unit:watch"],
      "console": "integratedTerminal"
    }
  ]
}
```

## Common Testing Patterns

### Testing React Query Hooks

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return ({ children }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

it('should fetch data', async () => {
  render(<MyComponent />, { wrapper: createWrapper() });
  // ...
});
```

### Testing Components with Router

```typescript
// Router is already mocked in src/test/setup.ts
import { render } from '@testing-library/react';

it('should render', () => {
  render(<MyComponent />);
  // useRouter, usePathname, etc. are already mocked
});
```

### Testing Error Boundaries

```typescript
it('should display error message', async () => {
  vi.mocked(apiService.getData).mockRejectedValueOnce(
    new Error('API Error')
  );

  render(<MyComponent />);

  await waitFor(() => {
    expect(screen.getByText(/error/i)).toBeInTheDocument();
  });
});
```

## Troubleshooting

### Common Issues

**Issue:** Tests fail with "Cannot find module" error
```bash
# Solution: Ensure path aliases are configured in vitest.config.ts
resolve: {
  alias: {
    '@': path.resolve(__dirname, './src'),
  },
}
```

**Issue:** Mock not working
```bash
# Solution: Clear mocks before each test
beforeEach(() => {
  vi.clearAllMocks();
});
```

**Issue:** Async test timeout
```bash
# Solution: Increase timeout or use waitFor
await waitFor(() => {
  expect(element).toBeInTheDocument();
}, { timeout: 5000 });
```

## Resources

- [Vitest Documentation](https://vitest.dev/)
- [React Testing Library](https://testing-library.com/react)
- [Playwright Documentation](https://playwright.dev/)
- [Testing Best Practices](https://kentcdodds.com/blog/common-mistakes-with-react-testing-library)

## Continuous Improvement

### Adding New Tests

When adding new features:

1. **Write unit tests** for business logic and utilities
2. **Write component tests** for UI components
3. **Write E2E tests** for critical user flows
4. **Verify coverage** meets minimum thresholds
5. **Run all tests** before committing

### Maintaining Tests

- **Keep tests simple** and focused on one thing
- **Update tests** when refactoring code
- **Remove obsolete tests** when features are removed
- **Review test failures** in CI/CD and fix promptly
- **Monitor coverage trends** and improve low-coverage areas

---

**Happy Testing! 🧪**
