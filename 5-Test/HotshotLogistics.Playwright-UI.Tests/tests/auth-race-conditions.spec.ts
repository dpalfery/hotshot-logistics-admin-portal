import { test, expect } from '@playwright/test';

/**
 * Race condition tests for authentication system.
 * These tests verify that the authentication guard system properly handles
 * race conditions and prevents unauthorized access.
 */
test.describe('Authentication Race Conditions', () => {

  test('should prevent API calls before MSAL initialization', async ({ page }) => {
    const apiRequests: any[] = [];

    // Track all API requests
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiRequests.push({
          url: request.url(),
          method: request.method(),
          timestamp: Date.now(),
          hasAuth: !!request.headers()['authorization']
        });
      }
    });

    // Navigate quickly to trigger potential race conditions
    await page.goto('/');

    // Wait a bit and then navigate to another page
    await page.waitForTimeout(500);
    await page.goto('/jobs');

    // Wait for page to stabilize
    await page.waitForLoadState('networkidle');

    // Check that no API calls were made without authentication
    const unauthenticatedCalls = apiRequests.filter(req => !req.hasAuth);
    expect(unauthenticatedCalls.length).toBe(0);

    // All API calls should have authentication headers
    const authenticatedCalls = apiRequests.filter(req => req.hasAuth);
    if (apiRequests.length > 0) {
      expect(authenticatedCalls.length).toBe(apiRequests.length);
    }
  });

  test('should handle rapid page navigation during authentication', async ({ page }) => {
    const navigationHistory: string[] = [];

    // Track navigation
    page.on('framenavigated', frame => {
      if (frame === page.mainFrame()) {
        navigationHistory.push(frame.url());
      }
    });

    // Rapid navigation sequence
    await page.goto('/');
    await page.goto('/jobs');
    await page.goto('/drivers');
    await page.goto('/customers');
    await page.goto('/');

    // Wait for final navigation to complete
    await page.waitForLoadState('networkidle');

    // Should end up at the final destination
    await expect(page).toHaveURL('/');

    // Should be authenticated throughout
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should prevent component rendering before authentication ready', async ({ page }) => {
    // Navigate to a page that loads quickly
    await page.goto('/');

    // Check that we don't see protected content before auth is ready
    const loadingIndicator = page.getByText('Authenticating');
    const protectedContent = page.locator('[data-testid="user-profile"]');

    // Should show loading, not protected content initially
    await expect(loadingIndicator).toBeVisible();

    // Eventually should show protected content
    await expect(protectedContent).toBeVisible();

    // Loading should be gone
    await expect(loadingIndicator).not.toBeVisible();
  });

  test('should handle authentication state changes during API calls', async ({ page }) => {
    const apiRequests: any[] = [];

    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiRequests.push({
          url: request.url(),
          timestamp: Date.now(),
          hasAuth: !!request.headers()['authorization']
        });
      }
    });

    await page.goto('/jobs');

    // Wait for initial API calls
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    const initialApiCalls = apiRequests.length;

    // Simulate token refresh by clearing MSAL cache
    await page.evaluate(() => {
      const keys = Object.keys(localStorage).filter(key => key.includes('msal'));
      keys.forEach(key => localStorage.removeItem(key));
    });

    // Trigger more API calls
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Should still have authentication on all API calls
    const allCalls = apiRequests.filter(req => req.hasAuth);
    expect(allCalls.length).toBe(apiRequests.length);
  });

  test('should handle concurrent API requests correctly', async ({ page }) => {
    const apiRequests: any[] = [];

    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiRequests.push({
          url: request.url(),
          method: request.method(),
          hasAuth: !!request.headers()['authorization']
        });
      }
    });

    await page.goto('/');

    // Wait for authentication to be ready
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();

    // Trigger multiple API calls simultaneously by navigating to dashboard
    await page.goto('/');

    // Wait for all API calls to complete
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // All API calls should have been authenticated
    const unauthenticatedCalls = apiRequests.filter(req => !req.hasAuth);
    expect(unauthenticatedCalls.length).toBe(0);

    // Should have made multiple API calls
    expect(apiRequests.length).toBeGreaterThan(0);
  });

  test('should prevent access during authentication initialization', async ({ page }) => {
    // Clear any cached auth state
    await page.context().clearCookies();
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });

    // Navigate to protected route
    await page.goto('/jobs');

    // Should redirect to login or show loading
    const currentUrl = page.url();
    const isOnLoginPage = currentUrl.includes('/login');
    const showsLoading = await page.getByText('Authenticating').isVisible().catch(() => false);

    expect(isOnLoginPage || showsLoading).toBe(true);
  });

  test('should handle browser back/forward during authentication', async ({ page }) => {
    // Start fresh
    await page.goto('/');

    // Navigate to jobs
    await page.goto('/jobs');
    await expect(page).toHaveURL('/jobs');

    // Go back
    await page.goBack();
    await expect(page).toHaveURL('/');

    // Go forward
    await page.goForward();
    await expect(page).toHaveURL('/jobs');

    // Should remain authenticated throughout
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should handle page refresh during authentication flow', async ({ page }) => {
    await page.goto('/');

    // Refresh during loading
    await page.reload();

    // Should still authenticate properly
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should prevent multiple simultaneous authentication attempts', async ({ page }) => {
    // This test verifies that the auth system doesn't get confused
    // by multiple rapid authentication triggers

    await page.goto('/');

    // Rapid reloads
    for (let i = 0; i < 3; i++) {
      await page.reload();
      await page.waitForLoadState('networkidle');
    }

    // Should still be in a consistent authenticated state
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should handle network interruptions during authentication', async ({ page }) => {
    // Mock network failure during auth
    let requestCount = 0;
    await page.route('**/api/**', route => {
      requestCount++;
      if (requestCount === 1) {
        // Fail first request
        route.abort();
      } else {
        // Succeed subsequent requests
        route.fulfill({ status: 200, body: '[]' });
      }
    });

    await page.goto('/');

    // Should recover from network failure
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

});