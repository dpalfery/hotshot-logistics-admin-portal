import { Page, BrowserContext } from '@playwright/test';

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
 * Uses Resource Owner Password Credentials (ROPC) flow for test accounts.
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
   * Check if real auth testing is enabled (credentials are available).
   */
  static isRealAuthEnabled(): boolean {
    return !!(process.env.E2E_TEST_USER_EMAIL && process.env.E2E_TEST_USER_PASSWORD);
  }

  /**
   * Authenticate using the Azure AD login page (interactive flow).
   * This simulates a real user login through the browser.
   */
  async loginInteractive(credentials: AuthCredentials): Promise<void> {
    // Navigate to login page which should trigger MSAL redirect
    await this.page.goto('/login');
    
    // Click the login button to start the auth flow
    const loginButton = this.page.getByRole('button', { name: /login|sign in/i });
    if (await loginButton.isVisible()) {
      await loginButton.click();
    }

    // Wait for redirect to Azure AD login page
    await this.page.waitForURL(/login\.microsoftonline\.com/, { timeout: 10000 });

    // Fill in email
    await this.page.waitForSelector('input[type="email"]', { timeout: 10000 });
    await this.page.fill('input[type="email"]', credentials.email);
    await this.page.click('input[type="submit"]');

    // Wait for password page (Azure AD does email first, then password)
    await this.page.waitForSelector('input[type="password"]', { timeout: 10000 });
    await this.page.fill('input[type="password"]', credentials.password);
    await this.page.click('input[type="submit"]');

    // Handle "Stay signed in?" prompt if it appears
    try {
      const staySignedInButton = this.page.locator('input[value="No"]');
      if (await staySignedInButton.isVisible({ timeout: 3000 })) {
        await staySignedInButton.click();
      }
    } catch {
      // Prompt didn't appear, continue
    }

    // Wait for redirect back to app
    await this.page.waitForURL(/localhost:3000/, { timeout: 15000 });

    // Verify authentication succeeded by checking for authenticated content
    await this.page.waitForSelector('[data-testid="authenticated-content"], nav, h1', { timeout: 10000 });
  }

  /**
   * Get token using ROPC flow (Resource Owner Password Credentials).
   * This is faster than interactive login but requires ROPC to be enabled in Azure AD.
   * 
   * NOTE: ROPC is NOT recommended for production but is acceptable for test accounts.
   * You must enable "Allow public client flows" in Azure AD app registration.
   */
  async getTokenViaROPC(credentials: AuthCredentials): Promise<AuthState> {
    const clientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
    const tenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

    if (!clientId || !tenantId) {
      throw new Error('Azure AD configuration missing. Set NEXT_PUBLIC_AZURE_CLIENT_ID and NEXT_PUBLIC_AZURE_TENANT_ID.');
    }

    const tokenEndpoint = `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/token`;

    const params = new URLSearchParams({
      client_id: clientId,
      scope: 'openid profile email User.Read',
      username: credentials.email,
      password: credentials.password,
      grant_type: 'password',
    });

    const response = await fetch(tokenEndpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: params.toString(),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`ROPC token request failed: ${response.status} ${error}`);
    }

    const tokenResponse = await response.json();

    return {
      accessToken: tokenResponse.access_token,
      expiresOn: Date.now() + (tokenResponse.expires_in * 1000),
      account: {
        username: credentials.email,
        name: 'Test User',
      },
    };
  }

  /**
   * Inject authentication state into the browser.
   * This allows tests to skip the login flow and start authenticated.
   */
  async injectAuthState(authState: AuthState): Promise<void> {
    // Inject token into sessionStorage to simulate MSAL cache
    await this.page.addInitScript((state: AuthState) => {
      // Store auth state that the app can use
      sessionStorage.setItem('e2e-auth-state', JSON.stringify(state));
      
      // Set flag to indicate E2E auth mode
      (window as Record<string, unknown>).__E2E_AUTH_STATE__ = state;
    }, authState);
  }

  /**
   * Save authentication state to a file for reuse across tests.
   */
  async saveAuthState(filePath: string): Promise<void> {
    await this.context.storageState({ path: filePath });
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
