import { FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('Minimal global setup running');
}

export default globalSetup;
