'use client';

import { useIsAuthenticated } from '@azure/msal-react';
import { useRouter } from 'next/navigation';
import { useEffect, ComponentType } from 'react';

const withAuth = <P extends object>(WrappedComponent: ComponentType<P>) => {
  const WithAuthComponent = (props: P) => {
    const isAuthenticated = useIsAuthenticated();
    const router = useRouter();

    // Check for E2E test auth state injected by Playwright tests
    const hasE2EAuthState = typeof window !== 'undefined' && 
      !!(window as unknown as Record<string, unknown>).__E2E_AUTH_STATE__;

    // Always allow access in development mode unless forced
    const isDevelopment = process.env.NODE_ENV === 'development';
    const forceAuth = process.env.NEXT_PUBLIC_FORCE_AUTH === 'true' || 
      (typeof window !== 'undefined' && (window as any).__FORCE_AUTH__ === true);

    useEffect(() => {
      // Only check authentication in production (and not in E2E test mode)
      // Or if forceAuth is enabled
      if ((!isDevelopment || forceAuth) && !isAuthenticated && !hasE2EAuthState) {
        router.push('/login');
      }
    }, [isAuthenticated, isDevelopment, forceAuth, hasE2EAuthState, router]);

    // Always render component in development mode (unless forced), E2E mode, or when authenticated
    if ((isDevelopment && !forceAuth) || isAuthenticated || hasE2EAuthState) {
      return <WrappedComponent {...props} />;
    }

    // Only block in production when not authenticated
    return null;
  };

  return WithAuthComponent;
};

export default withAuth;
