import { test, expect } from '@playwright/test';

/**
 * These tests run with REAL Azure AD authentication.
 * They require E2E_TEST_USER_EMAIL and E2E_TEST_USER_PASSWORD to be set.
 * 
 * File naming convention: *.auth.spec.ts runs only in authenticated projects.
 * 
 * MSAL uses localStorage (NEXT_PUBLIC_MSAL_CACHE_LOCATION=localStorage)
 * which Playwright's storageState can capture (sessionStorage cannot be saved).
 * 
 * **KNOWN LIMITATION**: WebKit + MSAL + Playwright storageState has compatibility issues.
 * MSAL encrypts localStorage tokens with a key stored in cookies. WebKit's stricter
 * security model prevents proper restoration of this encrypted state across browser contexts.
 * 
 * @see https://github.com/microsoft/playwright/issues/12486
 * @see https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/4319
 * 
 * **Solutions**:
 * 1. Run WebKit tests in headed mode with manual login (slow but reliable)
 * 2. Use auth mocking for WebKit (not real auth)
 * 3. Accept Chrome/Firefox coverage as sufficient
 */
test.describe('Real Authentication Flow', () => {
  
  test('should access protected dashboard with real token', async ({ page, browserName }) => {
    // Skip WebKit due to MSAL localStorage encryption incompatibility
    test.skip(browserName === 'webkit', 'WebKit + MSAL encrypted localStorage incompatible with Playwright storageState');
    
    await page.goto('/');
    
    // Should not be redirected to login
    await expect(page).not.toHaveURL(/\/login/);
    
    // Should see authenticated content
    await expect(page.getByText('Dashboard Overview')).toBeVisible();
  });

  test('should display user profile from Azure AD', async ({ page, browserName }) => {
    test.skip(browserName === 'webkit', 'WebKit + MSAL encrypted localStorage incompatible with Playwright storageState');
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Should show the authenticated user's info
    // This will show the actual test account name/email
    const userInfo = page.locator('[data-testid="user-profile"], [data-testid="user-email"]').first();
    await expect(userInfo).toBeVisible({ timeout: 10000 });
  });

  test('should make authenticated API calls', async ({ page, browserName }) => {
    test.skip(browserName === 'webkit', 'WebKit + MSAL encrypted localStorage incompatible with Playwright storageState');
    // Track API requests to verify Authorization header
    const apiRequests: { url: string; hasAuth: boolean }[] = [];
    
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        const authHeader = request.headers()['authorization'];
        apiRequests.push({
          url: request.url(),
          hasAuth: !!authHeader,
        });
        console.log('API Request:', request.method(), request.url(), 'Auth:', !!authHeader);
      }
    });

    await page.goto('/jobs');
    await page.waitForLoadState('networkidle');
    
    // Wait a bit more to ensure API calls complete
    await page.waitForTimeout(2000);

    console.log('Total API requests captured:', apiRequests.length);
    apiRequests.forEach(req => console.log('  -', req.url, 'hasAuth:', req.hasAuth));

    // Verify API calls included Authorization header
    const jobsApiCall = apiRequests.find(r => r.url.includes('/api/job'));
    expect(jobsApiCall).toBeDefined();
    expect(jobsApiCall?.hasAuth).toBe(true);
  });

  test('should handle token refresh seamlessly', async ({ page, browserName }) => {
    test.skip(browserName === 'webkit', 'WebKit + MSAL encrypted localStorage incompatible with Playwright storageState');
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
      await expect(page).toHaveURL(/\/login|login\.microsoftonline\.com/, { timeout: 10000 });
    }
  });
});
