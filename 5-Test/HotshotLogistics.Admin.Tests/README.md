# Playwright E2E Tests

End-to-end tests for the Hotshot Logistics Admin Dashboard using Playwright with real Azure AD authentication.

## Prerequisites

- Node.js 20+ installed
- A test Azure AD user account
- Azure AD application configured for the admin dashboard

## Setup

### 1. Install Dependencies

```bash
cd 5-Test/HotshotLogistics.Admin.Tests
npm install
```

### 2. Configure Environment Variables

Create a `.env` file in this directory with the following variables:

```bash
# Test User Credentials
E2E_TEST_USER_EMAIL=your-test-user@yourdomain.com
E2E_TEST_USER_PASSWORD=your-test-user-password

# Azure AD Configuration (same as your admin dashboard)
NEXT_PUBLIC_AZURE_CLIENT_ID=your-azure-client-id
NEXT_PUBLIC_AZURE_TENANT_ID=your-azure-tenant-id
```

**Important:** 
- Never commit the `.env` file to version control
- Use a dedicated test account, not a production user
- The test account must have appropriate permissions in the application

### 3. Install Playwright Browsers

```bash
npx playwright install --with-deps
```

## Running Tests

### Run all tests
```bash
npm test
```

### Run tests in headed mode (see the browser)
```bash
npx playwright test --headed
```

### Run specific test file
```bash
npx playwright test tests/dashboard-overview.spec.ts
```

### Run tests in a specific browser
```bash
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit
```

### Run tests in UI mode (interactive)
```bash
npx playwright test --ui
```

### Debug a specific test
```bash
npx playwright test tests/auth.spec.ts --debug
```

## How Authentication Works

1. **Global Setup**: Before tests run, the global setup script (`tests/global-setup.ts`) authenticates using your test credentials and saves the authentication state to `.auth-state.json`.

2. **Test Execution**: Each test loads the saved authentication state, so tests start already authenticated without needing to log in each time.

3. **Auth Methods**: The setup tries two methods:
   - **ROPC (Resource Owner Password Credentials)**: Faster, requires "Allow public client flows" enabled in Azure AD
   - **Interactive Login**: Fallback method that simulates manual login

## Test Structure

- `tests/auth.spec.ts` - Basic authentication flow tests
- `tests/auth-flow.auth.spec.ts` - Advanced authentication scenarios
- `tests/dashboard-overview.spec.ts` - Dashboard page tests
- `tests/jobs-management.spec.ts` - Jobs management tests
- `tests/drivers-management.spec.ts` - Driver management tests
- `tests/billing-management.spec.ts` - Billing tests
- `tests/tracking-dashboard.spec.ts` - Tracking dashboard tests

## Troubleshooting

### Tests are being skipped
Make sure all environment variables are set correctly. Check the console output for authentication setup messages.

### Authentication fails with ROPC
If you see errors about ROPC not being available:
1. Go to Azure Portal → App Registrations → Your App
2. Navigate to Authentication → Advanced settings
3. Enable "Allow public client flows"

Alternatively, the tests will fall back to interactive login automatically.

### Timeout errors
- Increase timeout in `playwright.config.ts` if needed
- Check that your dev server starts successfully
- Ensure your Azure AD configuration is correct

### Browser not found
Run `npx playwright install --with-deps` to install browser dependencies.

## Best Practices

1. **Test User**: Use a dedicated test account with realistic permissions
2. **Test Data**: Tests use API mocking for predictable results
3. **Parallel Execution**: Tests run in parallel by default; use `workers: 1` in config if needed
4. **Screenshots**: Automatically captured on failure in `test-results/` folder
5. **Traces**: Available for failed tests, view with `npx playwright show-trace`

## Reports

After test execution, view the HTML report:
```bash
npx playwright show-report
```

The report shows:
- Test results and duration
- Screenshots and videos (on failure)
- Trace files for debugging
- Browser console logs
