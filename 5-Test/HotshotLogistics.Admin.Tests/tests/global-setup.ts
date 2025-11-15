import { chromium } from '@playwright/test';
import { execSync } from 'node:child_process';

async function launchBrowserWithTimeout(timeoutMs: number): Promise<void> {
  const launchPromise = chromium.launch();
  const timeoutPromise = new Promise<never>((_, reject) =>
    setTimeout(() => reject(new Error('Browser launch timed out')), timeoutMs)
  );
  await Promise.race([launchPromise, timeoutPromise]);
  const browser = await launchPromise;
  await browser.close();
}

async function setup(): Promise<void> {
  try {
    console.log('Verifying browser installation...');
    await launchBrowserWithTimeout(10_000);
    console.log('Browser verification successful.');
  } catch (error) {
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

export default setup;