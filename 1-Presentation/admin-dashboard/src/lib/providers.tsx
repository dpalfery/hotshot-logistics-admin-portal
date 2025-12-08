'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode, useEffect, useState } from 'react';
import { PublicClientApplication, EventType, EventMessage, AuthenticationResult } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { msalConfig } from '@/config/auth';
import { AuthProvider as GlobalAuthProvider } from '@/contexts/AuthContext';
import { AuthProvider } from '@/components/auth/auth-provider';
import { apiService } from '@/services/api';

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 1000 * 60 * 5, // 5 minutes
            retry: 1,
        },
    },
});

export const msalInstance = new PublicClientApplication(msalConfig);

msalInstance.addEventCallback((event: EventMessage) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
        const payload = event.payload as AuthenticationResult;
        const account = payload.account;
        msalInstance.setActiveAccount(account);
    }
});

interface ProvidersProps {
    children: ReactNode;
}

export function Providers({ children }: ProvidersProps) {
    const [isMsalReady, setIsMsalReady] = useState(false);

    useEffect(() => {
        let isMounted = true;

        const initializeMsal = async () => {
            try {
                await msalInstance.initialize();
                if (!isMounted) {
                    return;
                }

                const accounts = msalInstance.getAllAccounts();
                if (!msalInstance.getActiveAccount() && accounts.length > 0) {
                    msalInstance.setActiveAccount(accounts[0]);
                }
                
                // Signal that authentication is ready when MSAL is initialized
                apiService.signalAuthReady();
            } catch (error) {
                console.error('MSAL initialization failed', error);
            } finally {
                if (isMounted) {
                    setIsMsalReady(true);
                }
            }
        };

        void initializeMsal();

        return () => {
            isMounted = false;
        };
    }, []);

    if (!isMsalReady) {
        return <div>Loading...</div>;
    }

    return (
        <MsalProvider instance={msalInstance}>
            <GlobalAuthProvider>
                <QueryClientProvider client={queryClient}>
                    <AuthProvider>
                        {children}
                    </AuthProvider>
                </QueryClientProvider>
            </GlobalAuthProvider>
        </MsalProvider>
    );
}