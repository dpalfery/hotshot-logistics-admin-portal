import type { NextConfig } from "next";
import { generateCspHeader } from "./src/lib/csp";

const nextConfig: NextConfig = {
  // Enable static export for Azure Static Web Apps
  output: 'export',

  // Disable image optimization for static export
  images: {
    unoptimized: true,
  },

  // Production HTTPS enforcement
  async headers() {
    // Get API URL from environment (must be set at build time for static export)
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || '';
    const isDevelopment = process.env.NODE_ENV === 'development';

    // Generate CSP header using centralized utility
    const cspHeader = generateCspHeader({
      apiUrl,
      isDevelopment,
    });

    return [
      {
        // Apply security headers to all routes
        source: '/(.*)',
        headers: [
          // HTTPS enforcement
          {
            key: 'Strict-Transport-Security',
            value: 'max-age=31536000; includeSubDomains; preload'
          },
          // Prevent clickjacking
          {
            key: 'X-Frame-Options',
            value: 'DENY'
          },
          // Prevent MIME type sniffing
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff'
          },
          // XSS protection
          {
            key: 'X-XSS-Protection',
            value: '1; mode=block'
          },
          // Referrer policy
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin'
          },
          // Content Security Policy for Azure AD authentication and API access
          // Generated with secure defaults (no unsafe-inline, no unsafe-eval in production)
          {
            key: 'Content-Security-Policy',
            value: cspHeader
          }
        ]
      }
    ];
  },

  // Redirect HTTP to HTTPS in production
  async redirects() {
    return [
      {
        source: '/.well-known/acme-challenge/:path*',
        destination: '/.well-known/acme-challenge/:path*',
        permanent: false,
      },
      // Redirect HTTP to HTTPS for all other routes
      {
        source: '/(.*)',
        has: [
          {
            type: 'header',
            key: 'x-forwarded-proto',
            value: 'http',
          }
        ],
        destination: 'https://:splat',
        permanent: true,
      }
    ];
  },

  // Production optimizations
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production',
  },

  // Enable experimental features for better security
  experimental: {
    scrollRestoration: true,
  },

  // Webpack configuration for production
  webpack: (config, { isServer }) => {
    if (!isServer) {
      // Client-side webpack config
      config.resolve.fallback = {
        ...config.resolve.fallback,
        fs: false,
        net: false,
        tls: false,
      };
    }
    return config;
  },
};

export default nextConfig;
