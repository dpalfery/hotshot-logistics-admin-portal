import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Enable static export for Azure Static Web Apps
  output: 'export',

  // Disable image optimization for static export
  images: {
    unoptimized: true,
  },

  // Production optimizations
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production',
  },

  // Enable experimental features for better security
  experimental: {
    scrollRestoration: true,
  },

  // Silence Next.js 16 Turbopack/webpack warning by explicitly providing an empty Turbopack config
  // (we no longer customize webpack here)
  turbopack: {},
};

export default nextConfig;
