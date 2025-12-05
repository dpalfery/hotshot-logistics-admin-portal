import { chromium, FullConfig } from '@playwright/test';
import { execSync } from 'node:child_process';
import { AuthHelper } from './utils/auth-helper';
import * as path from 'path';
import * as fs from 'fs';

const AUTH_STATE_PATH = path.join(__dirname, '.auth-state.json');

async function launchBrowserWithTimeout(timeoutMs: number): Promise<void> {
  const launchPromise = chromium.launch();
  const timeoutPromise = new Promise<never>((_, reject) =>
    setTimeout(() => reject(new Error('Browser launch timed out')), timeoutMs)
  );
  await Promise.race([launchPromise, timeoutPromise]);
  const browser = await launchPromise;
  await browser.close();
}

async function setupBrowsers(): Promise<void> {
  try {
    console.log('Verifying browser installation...');
    await launchBrowserWithTimeout(10_000);
    console.log('Browser verification successful.');
  } catch {
    console.warn('Browser not found, installing Playwright browsers...');
    try {
      execSync('npx playwright install --with-deps', { timeout: 300_000, stdio: 'inherit' });
      console.log('Browser installation completed, retrying launch...');
      await launchBrowserWithTimeout(10_000);
      console.log('Browser verification successful after installation.');
    } catch (installError) {
      const message = installError instanceof Error ? installError.message : String(installError);
      throw new Error(`Failed to install and verify browsers: ${message}`);
    }
  }
}

async function setupAuthentication(config: FullConfig): Promise<void> {
  // Skip if real auth is not enabled
  if (!AuthHelper.isRealAuthEnabled()) {
    console.log('\n⚠️  Authentication not configured');
    console.log('   Playwright tests require real Azure AD authentication.');
    console.log('   ');
    console.log('   Please set the following environment variables:');
    console.log('   - E2E_TEST_USER_EMAIL: Your test user email');
    console.log('   - E2E_TEST_USER_PASSWORD: Your test user password');
    console.log('   - NEXT_PUBLIC_AZURE_CLIENT_ID: Your Azure AD client ID');
    console.log('   - NEXT_PUBLIC_AZURE_TENANT_ID: Your Azure AD tenant ID');
    console.log('   ');
    console.log('   Tests will be skipped until authentication is configured.\n');
    return;
  }

  console.log('\n🔐 Auth mode: REAL (using test account)');
  console.log('   Setting up authenticated session...');

  const baseURL = config.projects[0]?.use?.baseURL || 'http://localhost:3000';
  const browser = await chromium.launch();
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  const authHelper = new AuthHelper(page, context);

  try {
    const credentials = AuthHelper.getTestCredentials();
    const baseURL = config.projects[0]?.use?.baseURL || 'http://localhost:3000';

    // Try ROPC first (faster), fall back to interactive if not enabled
    try {
      console.log('   Attempting ROPC token acquisition...');
      const authState = await authHelper.getTokenViaROPC(credentials);
      
      // Navigate to app and inject state
      await page.goto(baseURL);
      await authHelper.injectAuthState(authState);
      
      // Save browser storage state
      await authHelper.saveAuthState(AUTH_STATE_PATH);
      console.log('   ✅ Auth setup complete via ROPC.\n');
    } catch (ropcError) {
      console.log('   ROPC not available, using interactive login...');
      
      // Fall back to interactive login
      await authHelper.loginInteractive(credentials);
      await authHelper.saveAuthState(AUTH_STATE_PATH);
      console.log('   ✅ Auth setup complete via interactive login.\n');
    }
  } catch (error) {
    console.error('   ❌ Auth setup failed:', error);
    console.log('   Tests will run in bypass mode.\n');
    // Delete any stale auth state
    if (fs.existsSync(AUTH_STATE_PATH)) {
      fs.unlinkSync(AUTH_STATE_PATH);
    }
  } finally {
    await browser.close();
  }
}

async function globalSetup(config: FullConfig): Promise<void> {
  await setupBrowsers();
  await setupAuthentication(config);
}

export default globalSetup;
export { AUTH_STATE_PATH };