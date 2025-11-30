import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';
import { AuthHelper } from './tests/utils/auth-helper';

const AUTH_STATE_PATH = path.join(__dirname, 'tests', '.auth-state.json');

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  testMatch: /.*\.spec\.ts$/,
  /* Fail fast if a single test takes too long */
  timeout: 45_000,
  /* Global timeout for the full run */
  globalTimeout: 60 * 60 * 1000,
  /* Expect timeout */
  expect: { timeout: 10_000 },
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: process.env.CI
    ? [['dot'], ['html']]
    : [['list'], ['html']],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:3000',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    /* Action timeout */
    actionTimeout: 15_000,
  },
  /* Global setup - browser verification + optional auth */
  globalSetup: './tests/global-setup.ts',

  /* Configure projects for major browsers */
  projects: [
    // ============================================
    // BYPASS AUTH PROJECTS (fast, for most tests)
    // ============================================
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      testIgnore: /.*\.auth\.spec\.ts$/, // Ignore auth-specific tests
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
      testIgnore: /.*\.auth\.spec\.ts$/,
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
      testIgnore: /.*\.auth\.spec\.ts$/,
    },

    // ============================================
    // REAL AUTH PROJECT (for auth flow tests)
    // Runs only when E2E_TEST_USER_EMAIL and E2E_TEST_USER_PASSWORD are set
    // ============================================
    {
      name: 'chromium-authenticated',
      use: {
        ...devices['Desktop Chrome'],
        // Use saved auth state from global setup (if real auth is enabled)
        storageState: AuthHelper.isRealAuthEnabled() ? AUTH_STATE_PATH : undefined,
      },
      testMatch: /.*\.auth\.spec\.ts$/, // Only run auth-specific tests
    },

    /* Test against mobile viewports. */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
    // {
    //   name: 'Mobile Safari',
    //   use: { ...devices['iPhone 12'] },
    // },

    /* Test against branded browsers. */
    // {
    //   name: 'Microsoft Edge',
    //   use: { ...devices['Desktop Edge'], channel: 'msedge' },
    // },
    // {
    //   name: 'Google Chrome',
    //   use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    // },
  ],

  /* Run your local dev server before starting the tests */
  webServer: {
    command: 'npx cross-env NEXT_PUBLIC_AZURE_CLIENT_ID=test-client-id NEXT_PUBLIC_AZURE_TENANT_ID=test-tenant-id npx next dev',
    cwd: '../../1-Presentation/admin-dashboard',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
    timeout: process.env.CI ? 180_000 : 60_000,
  },
});