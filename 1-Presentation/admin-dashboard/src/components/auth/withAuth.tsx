'use client';

import { useIsAuthenticated } from '@azure/msal-react';
import { useRouter } from 'next/navigation';
import { useEffect, ComponentType } from 'react';

const withAuth = <P extends object>(WrappedComponent: ComponentType<P>) => {
  const WithAuthComponent = (props: P) => {
    const isAuthenticated = useIsAuthenticated();
    const router = useRouter();

    // Always allow access in development mode
    const isDevelopment = process.env.NODE_ENV === 'development';

    useEffect(() => {
      // Only check authentication in production
      if (!isDevelopment && !isAuthenticated) {
        router.push('/login');
      }
    }, [isAuthenticated, isDevelopment, router]);

    // Always render component in development mode
    if (isDevelopment || isAuthenticated) {
      return <WrappedComponent {...props} />;
    }

    // Only block in production when not authenticated
    return null;

    return <WrappedComponent {...props} />;
  };

  return WithAuthComponent;
};

export default withAuth;
