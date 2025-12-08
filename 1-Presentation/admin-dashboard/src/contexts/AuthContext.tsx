'use client';

import { createContext, useContext, useState, useEffect, ReactNode, useCallback, useMemo } from 'react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { InteractionStatus, EventType } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';
import { logger } from '@/lib/logger';

interface AuthState {
  isMsalReady: boolean;
  isAuthChecked: boolean;
  isAuthenticated: boolean;
  isTokenReady: boolean;
  user: User | null;
  error: Error | null;
}

interface AuthContextType extends AuthState {
  checkTokenReadiness: () => Promise<void>;
  resetAuthState: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [authState, setAuthState] = useState<AuthState>({
    isMsalReady: false,
    isAuthChecked: false,
    isAuthenticated: false,
    isTokenReady: false,
    user: null,
    error: null
  });

  const checkTokenReadiness = useCallback(async () => {
    try {
      // Use functional state update to access latest state without dependency
      setAuthState(prevState => {
        if (!prevState.isAuthenticated) {
          return { ...prevState, isTokenReady: false };
        }
        return prevState;
      });

      // We need to access the latest state, but we can't use authState in dependency array
      // because it changes on every update. Instead we rely on msalInstance and explicit logic.
      
      const account = msalInstance.getActiveAccount();
      if (!account) {
        logger.debug('No active MSAL account found during token check');
        setAuthState(prev => ({ ...prev, isTokenReady: false }));
        return;
      }

      // Attempt silent token acquisition to verify token readiness
      try {
        const response = await msalInstance.acquireTokenSilent({
          scopes: process.env.NEXT_PUBLIC_AZURE_AD_SCOPES?.split(' ') || [],
          account: account,
        });
        
        if (response.accessToken) {
          logger.debug('Token readiness verified successfully');
          setAuthState(prev => ({ ...prev, isTokenReady: true }));
        }
      } catch (error) {
        logger.warn('Token readiness check failed, but this is expected during initialization', {
          error: error instanceof Error ? error.message : 'Unknown error',
        });
        setAuthState(prev => ({ ...prev, isTokenReady: false }));
      }
    } catch (error) {
      logger.error('Error during token readiness check', {
        error: error instanceof Error ? error.message : 'Unknown error',
      });
      setAuthState(prev => ({ ...prev, isTokenReady: false, error: error instanceof Error ? error : new Error('Unknown error') }));
    }
  }, []); // Remove authState dependency to prevent infinite loops

  const resetAuthState = useCallback(() => {
    setAuthState({
      isMsalReady: false,
      isAuthChecked: false,
      isAuthenticated: false,
      isTokenReady: false,
      user: null,
      error: null
    });
  }, []);

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        // Check if MSAL is ready (not in progress)
        if (inProgress === InteractionStatus.None) {
          const accounts = msalInstance.getAllAccounts();
          
          if (accounts.length > 0) {
            msalInstance.setActiveAccount(accounts[0]);
            setAuthState(prev => ({
              ...prev,
              isMsalReady: true,
              isAuthChecked: true,
              isAuthenticated: true,
              user: accounts[0]
            }));
            
            // Check token readiness after MSAL is ready
            await checkTokenReadiness();
          } else {
            setAuthState(prev => ({
              ...prev,
              isMsalReady: true,
              isAuthChecked: true,
              isAuthenticated: false,
              isTokenReady: false
            }));
          }
        }
      } catch (error) {
        logger.error('Authentication initialization error', {
          error: error instanceof Error ? error.message : 'Unknown error',
        });
        setAuthState(prev => ({
          ...prev,
          error: error instanceof Error ? error : new Error('Unknown error')
        }));
      }
    };

    initializeAuth();
  }, [inProgress, isAuthenticated]);

  useEffect(() => {
    // Update authentication state when MSAL reports changes
    if (isAuthenticated !== authState.isAuthenticated) {
      setAuthState(prev => ({
        ...prev,
        isAuthenticated: isAuthenticated
      }));
      
      // Re-check token readiness when authentication state changes
      if (isAuthenticated) {
        checkTokenReadiness();
      }
    }
  }, [isAuthenticated]);

  const contextValue: AuthContextType = useMemo(() => ({
    ...authState,
    checkTokenReadiness,
    resetAuthState
  }), [authState, checkTokenReadiness, resetAuthState]);

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
