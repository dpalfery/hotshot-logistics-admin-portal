#!/usr/bin/env node

/**
 * Generate .env.local file from system environment variables
 * This script reads Azure AD configuration from system environment variables
 * and generates the .env.local file automatically for secure deployments.
 */

const fs = require('fs');
const path = require('path');

function generateEnvFile() {
  // Read required environment variables
  const azureClientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
  const azureTenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

  // Validate required environment variables
  if (!azureClientId || !azureTenantId) {
    console.warn('⚠️ Missing required environment variables for Azure AD.');
    console.warn('   - NEXT_PUBLIC_AZURE_CLIENT_ID');
    console.warn('   - NEXT_PUBLIC_AZURE_TENANT_ID');
    console.warn('Using placeholder values for build to proceed.');
    
    if (!azureClientId) process.env.NEXT_PUBLIC_AZURE_CLIENT_ID = "00000000-0000-0000-0000-000000000000";
    if (!azureTenantId) process.env.NEXT_PUBLIC_AZURE_TENANT_ID = "00000000-0000-0000-0000-000000000000";
  }

  const finalClientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
  const finalTenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

  // Application Insights
  const appInsightsConnectionString = process.env.NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING || '';

  const isProduction = process.env.NODE_ENV === 'production';

  const explicitApiBase = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
  const explicitApiUrl = process.env.NEXT_PUBLIC_API_URL?.trim();
  const productionDomain = process.env.NEXT_PUBLIC_PRODUCTION_DOMAIN?.trim();

  const normalizeDomain = (url) => url.replace(/\/$/, '');
  const ensureProtocol = (url) => (/^https?:\/\//i.test(url) ? url : `https://${url}`);
  const normalizeBase = (url) => ensureProtocol(normalizeDomain(url));
  const ensureApiPath = (url) => (url.toLowerCase().endsWith('/api') ? url : `${url}/api`);

  let apiBaseUrl;

  if (explicitApiBase) {
    apiBaseUrl = normalizeBase(explicitApiBase);
  } else if (explicitApiUrl) {
    apiBaseUrl = ensureApiPath(normalizeBase(explicitApiUrl));
  } else if (!isProduction) {
    apiBaseUrl = 'https://localhost:5001/api';
  } else if (productionDomain && productionDomain !== 'your-domain.com') {
    apiBaseUrl = ensureApiPath(normalizeBase(productionDomain));
  } else {
    throw new Error(
      'Missing NEXT_PUBLIC_API_BASE_URL or NEXT_PUBLIC_API_URL for production build. Set one of them to the Container App URL.'
    );
  }

  const resolvedProductionDomain = productionDomain || new URL(apiBaseUrl).host;

  // Generate .env.local content with production-ready configuration
  const envContent = `# API Configuration
NEXT_PUBLIC_API_BASE_URL=${apiBaseUrl}

# Azure AD Configuration (Auto-generated from system environment variables)
# These values are automatically populated from your system environment variables
NEXT_PUBLIC_AZURE_CLIENT_ID=${finalClientId}
NEXT_PUBLIC_AZURE_TENANT_ID=${finalTenantId}

# Application Insights
NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING=${appInsightsConnectionString}

# Production Configuration
NODE_ENV=${isProduction ? 'production' : 'development'}
NEXT_PUBLIC_PRODUCTION_DOMAIN=${resolvedProductionDomain}

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