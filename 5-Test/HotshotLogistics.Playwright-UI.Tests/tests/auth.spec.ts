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

});
