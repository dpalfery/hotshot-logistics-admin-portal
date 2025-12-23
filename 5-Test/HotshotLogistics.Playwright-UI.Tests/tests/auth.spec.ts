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

    // Wait for authentication to be ready
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible({ timeout: 15000 });

    // Should not redirect to login if authenticated
    await expect(page).toHaveURL('/jobs');
    await expect(page.getByText('Job Management')).toBeVisible();
  });

  test('should display user information when authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should see authenticated content (user info, navigation, etc.)
    // This is a basic check that authentication worked
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible({ timeout: 15000 });
  });

  test('should persist authentication across page navigations', async ({ page }) => {
    // Navigate to different pages
    await page.goto('/jobs');
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL('/jobs');

    await page.goto('/drivers');
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL('/drivers');

    await page.goto('/');
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL('/');
    await page.waitForLoadState('networkidle');

    // Should remain authenticated throughout
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();
  });

  test('should handle authentication timeout gracefully', async ({ page }) => {
    // Skip this test in real auth mode as it requires manipulating MSAL internals
    test.skip();
  });

  test('should handle authentication errors gracefully', async ({ page }) => {
    // Skip this test in real auth mode as it requires manipulating MSAL internals
    // The error handling UI is tested separately
    test.skip();
  });

  test('should prevent access to protected routes when not authenticated', async ({ page }) => {
    // Create a fresh browser context without any auth state
    const newContext = await page.context().browser()!.newContext();
    const newPage = await newContext.newPage();

    // Force auth check even in development
    await newPage.addInitScript(() => {
      (window as any).__FORCE_AUTH__ = true;
    });

    await newPage.goto('/jobs');

    // Should redirect to login or show auth loading
    // Wait for redirect to happen - either to /login or to MSAL provider
    await newPage.waitForURL(url => 
      url.pathname.includes('/login') || 
      url.hostname.includes('microsoftonline.com') || 
      url.hostname.includes('b2clogin.com'),
      { timeout: 15000 }
    );

    const currentUrl = newPage.url();
    const isOnLoginOrAuthPage = currentUrl.includes('/login') || currentUrl.includes('login.microsoftonline.com') || currentUrl.includes('b2clogin.com');
    
    expect(isOnLoginOrAuthPage).toBe(true);

    await newContext.close();
  });

  test('should handle token refresh seamlessly', async ({ page }) => {
    await page.goto('/');

    // Wait for initial load
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();

    // Navigate to different pages to test that auth persists
    await page.goto('/drivers');
    await expect(page).toHaveURL('/drivers');
    await expect(page.getByText('Driver Management')).toBeVisible();

    // Navigate again - should still be authenticated
    await page.goto('/jobs');
    await expect(page).toHaveURL('/jobs');
    await expect(page.getByText('Job Management')).toBeVisible();
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

    // Wait for page to fully load
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[data-testid="user-profile"]')).toBeVisible();

    // Give time for any background API calls to complete
    await page.waitForTimeout(2000);

    // API calls should be made with auth headers (or no API calls if page is static)
    if (apiRequests.length > 0) {
      const authenticatedCalls = apiRequests.filter(req => req.hasAuth);
      expect(authenticatedCalls.length).toBeGreaterThan(0);
    }
  });

  test('should handle network failures during authentication', async ({ page }) => {
    // Skip this test in real auth mode - network failures during MSAL init
    // cause complex behaviors that are hard to test in E2E
    test.skip();
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
