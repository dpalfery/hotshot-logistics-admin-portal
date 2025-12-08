import { test, expect } from '@playwright/test';
import { AuthHelper } from './utils/auth-helper';

/**
 * Basic authentication tests - these tests verify the auth system works correctly.
 * These tests now use real authentication when credentials are provided.
 */
test.describe('MSAL Authentication Flow', () => {

  test('should allow access to protected routes when authenticated', async ({ page }) => {
    // With real auth state loaded from global setup, user should be authenticated
    await page.goto('/jobs');

    // Should not redirect to login if authenticated
    await expect(page).toHaveURL('/jobs');
    await expect(page.getByText('Job Management')).toBeVisible();
  });

  test('should display user information when authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should see authenticated content (user info, navigation, etc.)
    // This is a basic check that authentication worked
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should persist authentication across page navigations', async ({ page }) => {
    // Navigate to different pages
    await page.goto('/jobs');
    await expect(page).toHaveURL('/jobs');

    await page.goto('/drivers');
    await expect(page).toHaveURL('/drivers');

    await page.goto('/');
    await expect(page).toHaveURL('/');
    await page.waitForLoadState('networkidle');

    // Should remain authenticated throughout
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should handle authentication timeout gracefully', async ({ page }) => {
    // Set a very short timeout to test timeout behavior
    await page.route('**/api/**', async route => {
      // Delay API responses to simulate slow authentication
      await new Promise(resolve => setTimeout(resolve, 15000));
      await route.fulfill({ status: 200, body: '[]' });
    });

    await page.goto('/');

    // Should show timeout error after 10 seconds
    await expect(page.getByText('Authentication Timeout')).toBeVisible({ timeout: 12000 });
    await expect(page.getByText('Refresh Page')).toBeVisible();
  });

  test('should handle authentication errors gracefully', async ({ page }) => {
    // Mock MSAL error
    await page.addScriptTag({
      content: `
        window.msalInstance = {
          ...window.msalInstance,
          acquireTokenSilent: () => Promise.reject(new Error('MSAL Error'))
        };
      `
    });

    await page.goto('/');

    // Should show error state
    await expect(page.getByText('Authentication Error')).toBeVisible();
    await expect(page.getByText('Retry Authentication')).toBeVisible();
  });

  test('should prevent access to protected routes when not authenticated', async ({ page }) => {
    // Clear any existing auth state
    await page.context().clearCookies();
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });

    await page.goto('/jobs');

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);
  });

  test('should handle token refresh seamlessly', async ({ page }) => {
    await page.goto('/');

    // Wait for initial load
    await page.waitForLoadState('networkidle');

    // Simulate token expiry by clearing localStorage MSAL cache
    await page.evaluate(() => {
      const keys = Object.keys(localStorage).filter(key => key.includes('msal'));
      keys.forEach(key => localStorage.removeItem(key));
    });

    // Navigate to trigger token refresh
    await page.goto('/drivers');

    // Should still be authenticated (token refresh should work)
    await expect(page).toHaveURL('/drivers');
    await expect(page.getByText('Driver Management')).toBeVisible();
  });

  test('should block API calls until authentication is ready', async ({ page }) => {
    const apiRequests: any[] = [];

    // Track API requests
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiRequests.push({
          url: request.url(),
          hasAuth: !!request.headers()['authorization']
        });
      }
    });

    await page.goto('/');

    // Make some interactions that would trigger API calls
    await page.waitForLoadState('networkidle');

    // Check that API calls include authorization headers
    const protectedApiCalls = apiRequests.filter(req => req.hasAuth);
    expect(protectedApiCalls.length).toBeGreaterThan(0);
  });

  test('should handle network failures during authentication', async ({ page }) => {
    // Mock network failure
    await page.route('**/api/**', route => route.abort());

    await page.goto('/');

    // Should handle network errors gracefully
    await expect(page.getByText('Authentication Error')).toBeVisible();
  });

  test('should maintain authentication state across browser refreshes', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();

    // Refresh the page
    await page.reload();

    // Should still be authenticated
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should handle concurrent authentication requests', async ({ page }) => {
    // Open multiple tabs/pages simultaneously
    const page2 = await page.context().newPage();

    await Promise.all([
      page.goto('/'),
      page2.goto('/jobs')
    ]);

    // Both should authenticate successfully
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
    await expect(page2.getByText('Job Management')).toBeVisible();

    await page2.close();
  });

});
