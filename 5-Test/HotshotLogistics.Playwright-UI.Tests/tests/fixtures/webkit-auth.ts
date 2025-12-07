/**
 * WebKit-specific test fixture
 * 
 * WebKit has known issues with storageState localStorage restoration.
 * This fixture uses addInitScript to inject localStorage BEFORE page load.
 * 
 * @see https://github.com/microsoft/playwright/issues/12486
 */

import { test as base } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

type WebKitFixtures = {
  page: any;
};

export const test = base.extend<WebKitFixtures>({
  page: async ({ page, browserName }, use) => {
    // Only apply fix for WebKit
    if (browserName === 'webkit') {
      console.log('[Fixture] WebKit detected, applying localStorage fix...');
      const authStatePath = path.join(__dirname, '..', '.auth-state.json');
      
      if (fs.existsSync(authStatePath)) {
        console.log('[Fixture] Auth state file found');
        const authState = JSON.parse(fs.readFileSync(authStatePath, 'utf-8'));
        
        // Find localhost:3000 origin
        const origin = authState.origins?.find((o: any) => 
          o.origin === 'http://localhost:3000'
        );
        
        if (origin && origin.localStorage) {
          console.log(`[Fixture] Found ${origin.localStorage.length} localStorage items`);
          
          // Navigate to app first to set the origin
          await page.goto('http://localhost:3000', { waitUntil: 'domcontentloaded' });
          
          // Inject localStorage directly into the page
          for (const item of origin.localStorage) {
            await page.evaluate(
              ([key, value]) => {
                localStorage.setItem(key, value);
              },
              [item.name, item.value]
            );
          }
          
          console.log('[Fixture] localStorage injected, reloading page...');
          // Reload so MSAL picks up the localStorage
          await page.reload({ waitUntil: 'load' });
          
          // Wait for MSAL to initialize (wait for Loading... to disappear)
          try {
            await page.waitForSelector('text=/Loading/i', { state: 'detached', timeout: 10000 });
            console.log('[Fixture] MSAL initialized successfully');
          } catch (e) {
            console.log('[Fixture] No Loading state detected or already gone');
          }
        } else {
          console.log('[Fixture] No localStorage found in auth state');
        }
      } else {
        console.log('[Fixture] Auth state file not found at:', authStatePath);
      }
    } else {
      console.log('[Fixture] Not WebKit, skipping fix');
    }
    
    await use(page);
  },
});

export { expect } from '@playwright/test';
