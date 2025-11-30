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
    if (AuthHelper.isRealAuthEnabled()) {
      await expect(page).toHaveURL('/jobs');
      await expect(page.getByText('Job Management')).toBeVisible();
    } else {
      console.log('Skipping test - authentication not configured');
      test.skip();
    }
  });

  test('should display user information when authenticated', async ({ page }) => {
    if (!AuthHelper.isRealAuthEnabled()) {
      test.skip();
      return;
    }

    await page.goto('/');
    
    // Should see authenticated content (user info, navigation, etc.)
    // This is a basic check that authentication worked
    await expect(page.locator('nav')).toBeVisible();
  });

  test('should persist authentication across page navigations', async ({ page }) => {
    if (!AuthHelper.isRealAuthEnabled()) {
      test.skip();
      return;
    }

    // Navigate to different pages
    await page.goto('/jobs');
    await expect(page).toHaveURL('/jobs');
    
    await page.goto('/drivers');
    await expect(page).toHaveURL('/drivers');
    
    await page.goto('/');
    await expect(page).toHaveURL('/');
    
    // Should remain authenticated throughout
    await expect(page.locator('nav')).toBeVisible();
  });

});
