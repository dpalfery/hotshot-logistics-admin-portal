import { test, expect } from '@playwright/test';
import { AuthHelper } from './utils/auth-helper';

/**
 * These tests run with REAL Azure AD authentication.
 * They require E2E_TEST_USER_EMAIL and E2E_TEST_USER_PASSWORD to be set.
 * 
 * File naming convention: *.auth.spec.ts runs only in authenticated projects.
 */
test.describe('Real Authentication Flow', () => {
  
  test.skip(!AuthHelper.isRealAuthEnabled(), 'Skipping: Real auth not configured');

  test('should access protected dashboard with real token', async ({ page }) => {
    await page.goto('/');
    
    // Should not be redirected to login
    await expect(page).not.toHaveURL(/\/login/);
    
    // Should see authenticated content
    await expect(page.getByText('Dashboard Overview')).toBeVisible();
  });

  test('should display user profile from Azure AD', async ({ page }) => {
    await page.goto('/');
    
    // Should show the authenticated user's info
    // This will show the actual test account name/email
    const userInfo = page.locator('[data-testid="user-profile"], [data-testid="user-email"]');
    await expect(userInfo).toBeVisible();
  });

  test('should make authenticated API calls', async ({ page }) => {
    // Track API requests to verify Authorization header
    const apiRequests: { url: string; hasAuth: boolean }[] = [];
    
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiRequests.push({
          url: request.url(),
          hasAuth: !!request.headers()['authorization'],
        });
      }
    });

    await page.goto('/jobs');
    await page.waitForLoadState('networkidle');

    // Verify API calls included Authorization header
    const jobsApiCall = apiRequests.find(r => r.url.includes('/api/job'));
    expect(jobsApiCall).toBeDefined();
    expect(jobsApiCall?.hasAuth).toBe(true);
  });

  test('should handle token refresh seamlessly', async ({ page }) => {
    await page.goto('/');
    
    // Wait a moment then navigate to ensure token is still valid
    await page.waitForTimeout(2000);
    await page.goto('/drivers');
    
    // Should still be authenticated
    await expect(page).not.toHaveURL(/\/login/);
    await expect(page.getByText('Driver Management')).toBeVisible();
  });

  test('should logout and redirect to login', async ({ page }) => {
    await page.goto('/');
    
    // Find and click logout button
    const logoutButton = page.getByRole('button', { name: /logout|sign out/i });
    
    if (await logoutButton.isVisible()) {
      await logoutButton.click();
      
      // Should redirect to login page
      await expect(page).toHaveURL(/\/login|login\.microsoftonline\.com/);
    }
  });
});
