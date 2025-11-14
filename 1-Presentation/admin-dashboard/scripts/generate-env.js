#!/usr/bin/env node

/**
 * Generate .env.local file from system environment variables
 * This script reads Azure AD configuration from system environment variables
 * and generates the .env.local file automatically for secure deployments.
 */

/* eslint-disable @typescript-eslint/no-require-imports */
const fs = require('fs');
const path = require('path');
/* eslint-enable @typescript-eslint/no-require-imports */

function generateEnvFile() {
  // Read required environment variables
  const azureClientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
  const azureTenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;
  
  // Check if we're in CI/test environment
  const isCI = process.env.CI === 'true' || process.env.NODE_ENV === 'test';

  // Validate required environment variables
  if (!azureClientId || !azureTenantId) {
    // In CI/test environments, use default test values
    if (isCI) {
      console.log('ℹ️  CI/test environment detected - using default test values');
      process.env.NEXT_PUBLIC_AZURE_CLIENT_ID = 'test-client-id';
      process.env.NEXT_PUBLIC_AZURE_TENANT_ID = 'test-tenant-id';
    } else {
      console.error('❌ Missing required environment variables:');
      if (!azureClientId) console.error('   - NEXT_PUBLIC_AZURE_CLIENT_ID');
      if (!azureTenantId) console.error('   - NEXT_PUBLIC_AZURE_TENANT_ID');
      console.error('\nPlease set these environment variables before running the build.');
      console.error('Example:');
      console.error('  export NEXT_PUBLIC_AZURE_CLIENT_ID="your-client-id"');
      console.error('  export NEXT_PUBLIC_AZURE_TENANT_ID="your-tenant-id"');
      process.exit(1);
    }
  }

  // Get final values (either from env or test defaults)
  const finalClientId = azureClientId || process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
  const finalTenantId = azureTenantId || process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

  // Validate that we're not using placeholder values (skip in CI/test)
  if (!isCI && (finalClientId.includes('your-') || finalTenantId.includes('your-'))) {
    console.error('❌ Environment variables still contain placeholder values.');
    console.error('Please replace placeholder values with actual Azure AD configuration.');
    process.exit(1);
  }

  // Determine environment and set appropriate URLs
  const isProduction = process.env.NODE_ENV === 'production';
  const productionDomain = process.env.NEXT_PUBLIC_PRODUCTION_DOMAIN || 'your-domain.com';
  const apiBaseUrl = isProduction
    ? `https://${productionDomain}/api`
    : 'https://localhost:5001/api';

  // Generate .env.local content with production-ready configuration
  const envContent = `# API Configuration
NEXT_PUBLIC_API_BASE_URL=${apiBaseUrl}

# Azure AD Configuration (Auto-generated from system environment variables)
# These values are automatically populated from your system environment variables
NEXT_PUBLIC_AZURE_CLIENT_ID=${finalClientId}
NEXT_PUBLIC_AZURE_TENANT_ID=${finalTenantId}

# Production Configuration
NODE_ENV=${isProduction ? 'production' : 'development'}
NEXT_PUBLIC_PRODUCTION_DOMAIN=${productionDomain}

# Security Configuration
# HTTPS is enforced in production via Next.js config and middleware

# Note: This file is auto-generated. Do not edit manually.
# Set NEXT_PUBLIC_AZURE_CLIENT_ID and NEXT_PUBLIC_AZURE_TENANT_ID as system environment variables instead.
# For production deployment, also set NEXT_PUBLIC_PRODUCTION_DOMAIN to your actual domain.
`;

  const envPath = path.join(__dirname, '..', '.env.local');

  try {
    // Write .env.local file
    fs.writeFileSync(envPath, envContent, 'utf8');
    console.log('✅ Successfully generated .env.local file');
    console.log(`   📄 File location: ${envPath}`);
    console.log(`   🔑 Azure Client ID: ${finalClientId.substring(0, 8)}...`);
    console.log(`   🏢 Azure Tenant ID: ${finalTenantId.substring(0, 8)}...`);
  } catch (error) {
    console.error('❌ Failed to write .env.local file:', error.message);
    process.exit(1);
  }
}

// Run the script
generateEnvFile();