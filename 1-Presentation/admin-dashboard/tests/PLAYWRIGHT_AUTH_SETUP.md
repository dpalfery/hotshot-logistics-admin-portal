# Playwright Authentication Setup Guide

## Changes Made

Removed `window.__BYPASS_AUTH__` test mode code from all production files for security reasons.

## How to Update Playwright Tests

### Option 1: Use MSAL Mock (Recommended)

Install the MSAL test utilities:
```bash
npm install --save-dev @azure/msal-node-extensions
```

Create a test fixture that mocks MSAL:

```typescript
// tests/fixtures/auth.fixture.ts
import { test as base } from '@playwright/test';

export const test = base.extend({
  page: async ({ page }, use) => {
    // Mock MSAL before navigating
    await page.addInitScript(() => {
      // Mock the MSAL instance
      (window as any).msalInstance = {
        getAllAccounts: () => [{
          username: 'test@example.com',
          name: 'Test User',
          localAccountId: 'test-id'
        }],
        getActiveAccount: () => ({
          username: 'test@example.com',
          name: 'Test User',
          localAccountId: 'test-id'
        }),
        setActiveAccount: () => {},
        acquireTokenSilent: async () => ({
          accessToken: 'mock-access-token',
          account: {
            username: 'test@example.com',
            name: 'Test User'
          }
        })
      };
    });

    await use(page);
  },
});

export { expect } from '@playwright/test';
```

Update your tests to use the fixture:

```typescript
// tests/dashboard-overview.spec.ts
import { test, expect } from './fixtures/auth.fixture';

test.describe('Dashboard Overview', () => {
  test.beforeEach(async ({ page }) => {
    // No longer need to set __BYPASS_AUTH__
    await page.goto('/');
  });

  test('should display dashboard metrics', async ({ page }) => {
    // Your test code...
  });
});
```

### Option 2: Mock API Responses (Alternative)

If you don't need real authentication flow testing:

```typescript
// tests/dashboard-overview.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Dashboard Overview', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API responses
    await page.route('**/api/**', async (route) => {
      const url = route.request().url();
      
      if (url.includes('/jobs')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [/* mock jobs */],
            totalCount: 10
          })
        });
      } else if (url.includes('/drivers')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([/* mock drivers */])
        });
      }
      // Add more routes as needed
    });

    await page.goto('/');
  });

  test('should display dashboard metrics', async ({ page }) => {
    // Your test code...
  });
});
```

### Option 3: Use Development Mode

Since development mode (`NODE_ENV === 'development'`) allows bypassing auth, you can:

1. Configure Playwright to run in development mode
2. Set `NEXT_PUBLIC_API_BASE_URL` to point to mock server

```typescript
// playwright.config.ts
export default defineConfig({
  use: {
    baseURL: 'http://localhost:3000',
  },
  webServer: {
    command: 'npm run dev', // Runs in development mode
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
  },
});
```

## Backend API Testing

For backend API testing that requires authentication:

```typescript
// Set up authenticated context once
const authFile = 'playwright/.auth/user.json';

test.use({ storageState: authFile });

test.beforeAll(async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Perform actual login
  await page.goto('/login');
  // ... login steps ...
  
  // Save auth state
  await page.context().storageState({ path: authFile });
  await context.close();
});
```

## Security Notes

- ✅ No more `__BYPASS_AUTH__` in production code
- ✅ Development mode still works locally (uses test tokens)
- ✅ Production builds require real authentication
- ✅ Tests mock authentication at the browser/network level

## Next Steps

1. Choose your preferred approach above
2. Update all `*.spec.ts` files to remove `window.__BYPASS_AUTH__ = true`
3. Implement the chosen mocking strategy
4. Run tests to verify: `npm run test:e2e`
