'use client';

import { useEffect, useState } from 'react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { useRouter, usePathname } from 'next/navigation';
import { InteractionStatus } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';
import { useAuth as useGlobalAuth } from '@/contexts/AuthContext';
import { logger } from '@/lib/logger';

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [isAuthChecked, setIsAuthChecked] = useState(false);
  
  // Use the global auth context
  const { 
    isMsalReady, 
    isTokenReady, 
    checkTokenReadiness, 
    resetAuthState,
    error
  } = useGlobalAuth();

  // Timeout handling
  const [isTimedOut, setIsTimedOut] = useState(false);
  useEffect(() => {
    const timer = setTimeout(() => {
      if (isLoading) {
        setIsTimedOut(true);
      }
    }, 10000); // 10 seconds timeout
    return () => clearTimeout(timer);
  }, [isLoading]);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        if (inProgress === InteractionStatus.None) {
          const accounts = msalInstance.getAllAccounts();
          if (accounts.length > 0) {
            msalInstance.setActiveAccount(accounts[0]);
          }
          setIsAuthChecked(true);
          
          // Check token readiness after MSAL initialization
          await checkTokenReadiness();
        }
      } catch (error) {
        logger.error('Authentication check failed', {
          error: error instanceof Error ? error.message : 'Unknown error',
        });
        setIsAuthChecked(true);
      }
    };
    checkAuth();
  }, [inProgress, checkTokenReadiness]);

  useEffect(() => {
    // Check authentication in all environments
    if (isAuthChecked && !isAuthenticated && pathname !== '/login') {
      const redirectUri = pathname !== '/' ? `?redirect_uri=${encodeURIComponent(pathname)}` : '';
      router.push(`/login${redirectUri}`);
    }
  }, [isAuthenticated, isAuthChecked, pathname, router]);

  // Enhanced loading state that considers both MSAL readiness and token readiness
  const isLoading = !isAuthChecked || 
                   (inProgress !== InteractionStatus.None && inProgress !== InteractionStatus.HandleRedirect) ||
                   !isMsalReady ||
                   (isAuthenticated && !isTokenReady);

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-red-900 mb-2">Authentication Error</h2>
          <p className="text-gray-600 mb-4">{error.message || 'An error occurred during authentication.'}</p>
          <button 
            onClick={() => {
              resetAuthState();
              window.location.reload();
            }}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
          >
            Retry Authentication
          </button>
        </div>
      </div>
    );
  }

  if (isTimedOut && isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Authentication Timeout</h2>
          <p className="text-gray-600 mb-4">Authentication is taking longer than expected. Please refresh the page.</p>
          <button 
            onClick={() => window.location.reload()} 
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Refresh Page
          </button>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500 mb-4"></div>
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Authenticating</h2>
          <p className="text-gray-600">Please wait while we verify your credentials...</p>
        </div>
      </div>
    );
  }

  // Prevent rendering protected content if not authenticated
  if (!isAuthenticated && pathname !== '/login') {
    return null; // Don't render anything while redirecting
  }

  return <>{children}</>;
}