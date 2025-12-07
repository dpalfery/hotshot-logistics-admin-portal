'use client';

import { useEffect, useState } from 'react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { useRouter, usePathname } from 'next/navigation';
import { InteractionStatus } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [isAuthChecked, setIsAuthChecked] = useState(false);

  useEffect(() => {
    const checkAuth = async () => {
      if (inProgress === InteractionStatus.None) {
        const accounts = msalInstance.getAllAccounts();
        if (accounts.length > 0) {
          msalInstance.setActiveAccount(accounts[0]);
        }
        setIsAuthChecked(true);
      }
    };
    checkAuth();
  }, [inProgress]);

  useEffect(() => {
    // Check authentication in all environments
    if (isAuthChecked && !isAuthenticated && pathname !== '/login') {
      const redirectUri = pathname !== '/' ? `?redirect_uri=${encodeURIComponent(pathname)}` : '';
      router.push(`/login${redirectUri}`);
    }
  }, [isAuthenticated, isAuthChecked, pathname, router]);

  if (!isAuthChecked || (inProgress !== InteractionStatus.None && inProgress !== InteractionStatus.HandleRedirect)) {
    return <div>Loading...</div>; // Or a proper loading spinner
  }

  return <>{children}</>;
}