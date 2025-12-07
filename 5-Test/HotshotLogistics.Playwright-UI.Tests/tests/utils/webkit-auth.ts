/**
 * WebKit-specific auth helper
 * 
 * WebKit has known issues with storageState localStorage restoration.
 * This helper explicitly sets localStorage items from the auth state file.
 * 
 * @see https://github.com/microsoft/playwright/issues/12486
 */

import type { Page } from 'playwright';
import * as fs from 'fs';
import * as path from 'path';

export class WebKitAuthHelper {
  /**
   * Explicitly restore localStorage from auth state for WebKit
   * Must be called after page.goto but before any auth checks
   */
  static async restoreLocalStorage(page: Page): Promise<void> {
    const authStatePath = path.join(__dirname, '..', '.auth-state.json');
    
    if (!fs.existsSync(authStatePath)) {
      console.warn('⚠️  Auth state file not found, skipping WebKit localStorage restoration');
      return;
    }

    const authState = JSON.parse(fs.readFileSync(authStatePath, 'utf-8'));
    
    // Find localhost:3000 origin
    const origin = authState.origins?.find((o: any) => 
      o.origin === 'http://localhost:3000'
    );
    
    if (!origin || !origin.localStorage) {
      console.warn('⚠️  No localStorage found in auth state for localhost:3000');
      return;
    }

    // Explicitly set each localStorage item
    for (const item of origin.localStorage) {
      await page.evaluate(
        ([key, value]) => {
          localStorage.setItem(key, value);
        },
        [item.name, item.value]
      );
    }

    console.log(`✅ Restored ${origin.localStorage.length} localStorage items for WebKit`);
  }
}
