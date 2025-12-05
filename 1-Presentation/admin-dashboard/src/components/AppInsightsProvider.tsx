'use client';

import { useEffect } from 'react';
import { appInsights } from '@/lib/appInsights';

export function AppInsightsProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    // This triggers the import and initialization of appInsights
    if (appInsights) {
      // Optional: Add any other client-side initialization here
    }
  }, []);

  return <>{children}</>;
}
