import { Page, BrowserContext } from 'playwright';

export interface AuthCredentials {
  email: string;
  password: string;
}

export interface AuthState {
  accessToken: string;
  expiresOn: number;
  account: {
    username: string;
    name: string;
  };
}

/**
 * Helper class for handling real MSAL authentication in E2E tests.
 */
export class AuthHelper {
  private page: Page;
  private context: BrowserContext;

  constructor(page: Page, context: BrowserContext) {
    this.page = page;
    this.context = context;
  }

  /**
   * Get test credentials from environment variables.
   * Throws if credentials are not configured.
   */
  static getTestCredentials(): AuthCredentials {
    const email = process.env.E2E_TEST_USER_EMAIL;
    const password = process.env.E2E_TEST_USER_PASSWORD;

    if (!email || !password) {
      throw new Error(
        'E2E test credentials not configured. ' +
        'Set E2E_TEST_USER_EMAIL and E2E_TEST_USER_PASSWORD environment variables.'
      );
    }

    return { email, password };
  }

  /**
   * Authenticate using the Azure AD login page (interactive flow).
   * This simulates a real user login through the browser.
   */
  async loginInteractive(credentials: AuthCredentials): Promise<void> {
    console.log('Starting interactive login flow...');
    
    // Navigate to login page which should trigger MSAL redirect
    console.log('Navigating to /login...');
    await this.page.goto('/login');
    
    // Wait for MSAL initialization to complete (Loading... text from Providers component should disappear)
    console.log('Waiting for MSAL initialization (Loading... state to clear)...');
    try {
      // Wait for the loading state to be detached (gone)
      await this.page.waitForSelector('text=Loading...', { state: 'detached', timeout: 15000 });
    } catch (e) {
      console.log('Warning: "Loading..." text did not detach within timeout, or was never present. Proceeding...');
    }

    // Ensure network is idle (scripts loaded)
    await this.page.waitForLoadState('networkidle');

    // Click the login button to start the auth flow
    // Try specific text match for "Login" or "Sign In"
    try {
      console.log('Looking for login button...');
      const loginButton = this.page.getByRole('button', { name: 'Login' });
      if (await loginButton.isVisible({ timeout: 5000 })) {
        console.log('Login button found, clicking...');
        await loginButton.click();
      } else {
        // Fallback to generic regex
        console.log('Specific "Login" button not found, trying generic regex...');
        const genericButton = this.page.getByRole('button', { name: /login|sign in/i });
        if (await genericButton.isVisible({ timeout: 5000 })) {
          console.log('Generic login button found, clicking...');
          await genericButton.click();
        } else {
          console.log('No login button found. Checking if redirect happened automatically...');
        }
      }
    } catch (e) {
      // If button is not found/clickable, we might already be redirecting
      console.log('Login button interaction failed or skipped:', e);
    }

    // Wait for redirect to Azure AD login page
    console.log('Waiting for redirect to login.microsoftonline.com...');
    await this.page.waitForURL(/login\.microsoftonline\.com/, { timeout: 60000 });
    console.log('Redirected to Azure AD.');

    // Fill in email
    console.log('Waiting for email input...');
    await this.page.waitForSelector('input[type="email"]', { timeout: 60000 });
    console.log('Filling email...');
    await this.page.fill('input[type="email"]', credentials.email);
    await this.page.click('input[type="submit"]');

    // Wait for password page (Azure AD does email first, then password)
    console.log('Waiting for password input...');
    await this.page.waitForSelector('input[type="password"]', { timeout: 30000 });
    console.log('Filling password...');
    await this.page.fill('input[type="password"]', credentials.password);
    
    // Use a more robust selector for the Sign In button (input, button, or specific ID)
    console.log('Clicking sign in...');
    await this.page.click('input[type="submit"], button[type="submit"], #idSIButton9');

    // Handle "Stay signed in?" prompt if it appears
    try {
      // Wait a bit to see if prompt appears
      await this.page.waitForTimeout(2000);
      
      // Try multiple selectors for the "No" button/option
      const staySignedInButton = this.page.getByRole('button', { name: /No/i }).or(this.page.locator('input[value="No"]'));
      
      if (await staySignedInButton.first().isVisible({ timeout: 5000 })) {
        console.log('Handling "Stay signed in" prompt...');
        await staySignedInButton.first().click();
      }
    } catch {
      // Prompt didn't appear or error interacting with it, continue
    }

    // Wait for redirect back to app
    // Note: If this times out, check for "Update your password" or MFA screens
    console.log('Waiting for redirect back to localhost:3000...');
    await this.page.waitForURL(/localhost:3000/, { timeout: 120000 });
    console.log('Redirected back to app URL.');

    // Allow time for MSAL to process the token from the URL hash
    console.log('Waiting for client-side auth processing...');
    await this.page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {}); // Ignore timeout on network idle
    await this.page.waitForTimeout(5000);

    // Check where we ended up
    const finalUrl = this.page.url();
    console.log(`Final URL after login flow: ${finalUrl}`);

    if (finalUrl.includes('/login')) {
      console.error('Redirected back to login page - Auth failed on client side.');
      // Dump local storage to see if MSAL state exists
      const storage = await this.page.evaluate(() => JSON.stringify(sessionStorage));
      console.log('Session Storage dump:', storage.substring(0, 200) + '...');
      
      // Take a screenshot
      const screenshotPath = `debug-screenshots/auth-failure-${Date.now()}.png`;
      await this.page.screenshot({ path: screenshotPath, fullPage: true });
      console.log(`Screenshot saved to: ${screenshotPath}`);

      throw new Error('Authentication failed: Redirected back to login page');
    }

    // Verify authentication succeeded by checking for authenticated content
    // We look for the Dashboard header or User Profile which are only visible when logged in
    console.log('Verifying authenticated content...');
    try {
      await this.page.waitForSelector('text=Dashboard Overview', { timeout: 30000 });
    } catch (e) {
      console.error('Authentication verification failed. Current URL:', this.page.url());
      
      // Take a screenshot
      const screenshotPath = `debug-screenshots/auth-verification-failure-${Date.now()}.png`;
      await this.page.screenshot({ path: screenshotPath, fullPage: true });
      console.log(`Screenshot saved to: ${screenshotPath}`);
      throw e;
    }
    console.log('Authentication successful.');
  }

  /**
   * Save authentication state to a file for reuse across tests.
   */
  async saveAuthState(filePath: string): Promise<void> {
    // Debug: Check what's in storage before saving
    const storageDebug = await this.page.evaluate(() => {
      return {
        sessionStorageKeys: Object.keys(sessionStorage),
        localStorageKeys: Object.keys(localStorage),
        sessionStorageCount: sessionStorage.length,
        localStorageCount: localStorage.length,
      };
    });
    console.log('Storage state before saving:',  JSON.stringify(storageDebug, null, 2));
    
    await this.context.storageState({ path: filePath });
    console.log(`Auth state saved to: ${filePath}`);
  }

  /**
   * Clear all authentication state.
   */
  async logout(): Promise<void> {
    await this.page.evaluate(() => {
      sessionStorage.clear();
      localStorage.clear();
    });
    await this.context.clearCookies();
  }
}
