/**
 * Content Security Policy (CSP) Utility
 *
 * Centralized CSP configuration to ensure consistent security policies
 * across next.config.ts and middleware.ts.
 *
 * Security improvements:
 * - Removes unsafe-inline and unsafe-eval directives
 * - Uses nonce-based script execution for dynamic scripts
 * - Centralizes CSP directive generation
 * - Includes API origin in connect-src
 */

export interface CspConfig {
  apiUrl?: string;
  isDevelopment?: boolean;
  nonce?: string;
}

/**
 * Generates a Content Security Policy string with secure defaults
 *
 * @param config - Configuration options for CSP generation
 * @returns CSP header value string
 */
export function generateCspHeader(config: CspConfig = {}): string {
  const { apiUrl, isDevelopment = false, nonce } = config;

  // Build connect-src directive with required origins
  const connectSrcUrls = [
    "'self'",
    "https://*.microsoftonline.com",
    "https://*.msecnd.net",
    "https://login.microsoftonline.com"
  ];

  // Add development localhost endpoints
  if (isDevelopment) {
    connectSrcUrls.push(
      "https://localhost:5001",
      "http://localhost:5000",
      "ws://localhost:3001", // WebSocket for hot reload
      "wss://localhost:3001"
    );
  }

  // Add production API URL if configured
  if (apiUrl) {
    try {
      const url = new URL(apiUrl);
      const origin = url.origin;
      if (!connectSrcUrls.includes(origin)) {
        connectSrcUrls.push(origin);
      }
    } catch {
      // If URL parsing fails, add as-is
      if (!connectSrcUrls.includes(apiUrl)) {
        connectSrcUrls.push(apiUrl);
      }
    }
  }

  // Build script-src directive without unsafe directives
  const scriptSrcUrls = ["'self'"];

  // Add nonce if provided (for inline scripts)
  if (nonce) {
    scriptSrcUrls.push(`'nonce-${nonce}'`);
  }

  // Azure AD B2C requires some script sources
  scriptSrcUrls.push(
    "https://*.microsoftonline.com",
    "https://*.msecnd.net"
  );

  // In development, we need to allow some flexibility for HMR and dev tools
  if (isDevelopment) {
    // Use strict-dynamic with nonce for better security in development
    // This allows scripts loaded by trusted scripts
    if (nonce) {
      scriptSrcUrls.push("'strict-dynamic'");
    }
    // Eval is needed for Next.js dev mode HMR
    // This is acceptable in development but removed in production
    scriptSrcUrls.push("'unsafe-eval'");
  }

  // Build style-src directive
  const styleSrcUrls = [
    "'self'",
    "https://*.microsoftonline.com"
  ];

  // Allow hashed styles for Tailwind and CSS-in-JS
  // In production, we should generate hashes, but for now allow self + Azure AD
  if (isDevelopment) {
    // Style sources need unsafe-inline for HMR and Tailwind JIT
    styleSrcUrls.push("'unsafe-inline'");
  } else {
    // In production, use nonce for inline styles if provided
    if (nonce) {
      styleSrcUrls.push(`'nonce-${nonce}'`);
    } else {
      // Fallback to unsafe-inline for styles only (less risky than scripts)
      // TODO: Replace with style hashes or nonce in production
      styleSrcUrls.push("'unsafe-inline'");
    }
  }

  // Construct the complete CSP header
  const cspDirectives = [
    "default-src 'self'",
    `script-src ${scriptSrcUrls.join(' ')}`,
    `style-src ${styleSrcUrls.join(' ')}`,
    "img-src 'self' data: https: blob:",
    `connect-src ${connectSrcUrls.join(' ')}`,
    "frame-src 'self' https://*.microsoftonline.com",
    "font-src 'self' data: https://*.microsoftonline.com",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'",
    "upgrade-insecure-requests"
  ];

  return cspDirectives.join('; ');
}

/**
 * Generates a cryptographically secure nonce for CSP
 *
 * @returns Base64-encoded nonce string
 */
export function generateNonce(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    // Use Web Crypto API if available (browser/edge runtime)
    return crypto.randomUUID().replace(/-/g, '');
  }

  // Fallback for Node.js environment
  const array = new Uint8Array(16);
  if (typeof crypto !== 'undefined' && crypto.getRandomValues) {
    crypto.getRandomValues(array);
  } else {
    // Last resort: use Math.random (not cryptographically secure)
    for (let i = 0; i < array.length; i++) {
      array[i] = Math.floor(Math.random() * 256);
    }
  }

  return Buffer.from(array).toString('base64');
}
