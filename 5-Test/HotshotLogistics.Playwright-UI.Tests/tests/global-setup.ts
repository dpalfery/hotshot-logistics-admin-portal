import { chromium } from 'playwright';
import { FullConfig } from '@playwright/test';
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
  console.log('\n🔐 Auth mode: REAL (using test account)');
  console.log('   Setting up authenticated session...');

  const baseURL = config.projects[0]?.use?.baseURL || 'http://localhost:3000';
  const browser = await chromium.launch();
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  const authHelper = new AuthHelper(page, context);

  try {
    // This will throw if credentials are not set, enforcing real auth
    const credentials = AuthHelper.getTestCredentials();
    // const baseURL = config.projects[0]?.use?.baseURL || 'http://localhost:3000'; // Removed as unused

    // Use interactive login only
    console.log('   Using interactive login...');
    await authHelper.loginInteractive(credentials);
    await authHelper.saveAuthState(AUTH_STATE_PATH);
    console.log('   ✅ Auth setup complete via interactive login.\n');

  } catch (error) {
    console.error('   ❌ Auth setup failed:', error);
    console.error('   Real authentication is required for tests. Please ensure credentials are set.');
    
    // Delete any stale auth state
    if (fs.existsSync(AUTH_STATE_PATH)) {
      fs.unlinkSync(AUTH_STATE_PATH);
    }
    
    // Fail the setup if auth fails
    throw error;
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