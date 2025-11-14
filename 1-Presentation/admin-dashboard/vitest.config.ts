import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html', 'lcov'],
      exclude: [
        'node_modules/',
        'src/test/',
        '**/*.d.ts',
        '**/*.config.*',
        '**/dist/**',
        '**/build/**',
        '**/.next/**',
        '**/coverage/**',
        '**/app/**', // Exclude Next.js app directory (pages)
        '**/config/**', // Exclude configuration files
        '**/lib/**', // Exclude lib utilities (providers, logger) - add tests later
        '**/components/auth/**', // Exclude auth components - add tests later
        '**/components/layout/**', // Exclude layout components
        '**/components/jobs/**', // Exclude job components - add tests later
        '**/components/drivers/**', // Exclude driver components - add tests later
        '**/components/billing/**', // Exclude billing components - add tests later
        '**/components/tracking/**', // Exclude tracking components - add tests later
        '**/components/dashboard/DashboardOverview.tsx', // Exclude until tests are written
        '**/services/signalr.ts', // Exclude SignalR service - add tests later
      ],
      thresholds: {
        lines: 60,
        functions: 60,
        branches: 60,
        statements: 60,
      },
    },
    include: ['**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    exclude: [
      'node_modules',
      'dist',
      '.next',
      'build',
      'coverage',
      'tests/**', // Exclude Playwright tests
    ],
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
