import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import { AuthProvider, useAuth } from '@/contexts/AuthContext';
import { apiService } from '@/services/api';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';

// Mock MSAL hooks
const mockUseMsal = useMsal as jest.MockedFunction<typeof useMsal>;
const mockUseIsAuthenticated = useIsAuthenticated as jest.MockedFunction<typeof useIsAuthenticated>;

// Mock fetch for API calls
const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;

describe('Authentication Flow Integration', () => {
  const mockAccount = {
    username: 'test@example.com',
    name: 'Test User',
    localAccountId: 'test-id',
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    // Reset API service auth state completely
    // We need to re-instantiate or reset private fields. 
    // Since it's a singleton, we need to be careful.
    // Ideally ApiService should expose a reset method for testing.
    // For now, we manually reset the promise.
    
    // @ts-ignore - Accessing private property for test reset
    apiService.authReadyPromise = new Promise((resolve) => {
      // @ts-ignore
      apiService.authReadyResolve = resolve;
    });
  });

  describe('Complete Authentication Flow', () => {
    it('should complete full authentication flow from MSAL init to API call', async () => {
      // Mock MSAL initialization
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      // Mock MSAL instance methods
      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'test-access-token',
      });

      // Mock API response
      const mockApiResponse = {
        data: [{ id: '1', title: 'Test Job' }],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 10,
      };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue(mockApiResponse),
      } as any);

      // Test component that uses the full auth flow
      const TestComponent = () => {
        const { isAuthenticated, isTokenReady } = useAuth();
        const [apiData, setApiData] = React.useState(null);

        React.useEffect(() => {
          if (isAuthenticated && isTokenReady) {
            // Signal auth ready to API service
            apiService.signalAuthReady();

            // Make API call
            apiService.getJobs().then(setApiData).catch(console.error);
          }
        }, [isAuthenticated, isTokenReady]);

        return (
          <div>
            <div data-testid="auth-status">
              Auth: {isAuthenticated ? 'true' : 'false'}, Token: {isTokenReady ? 'true' : 'false'}
            </div>
            {apiData && <div data-testid="api-data">API loaded</div>}
          </div>
        );
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Initially should show loading
      expect(screen.getByText('Loading...')).toBeInTheDocument();

      // Wait for authentication to complete
      await waitFor(() => {
        expect(screen.getByTestId('auth-status')).toHaveTextContent('Auth: true, Token: true');
      });

      // Should now show the protected content
      expect(screen.getByTestId('auth-status')).toBeInTheDocument();

      // Wait for API call to complete
      await waitFor(() => {
        expect(screen.getByTestId('api-data')).toHaveTextContent('API loaded');
      });

      // Verify API was called with correct authorization
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/job'),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer test-access-token',
          }),
        })
      );
    });

    it('should handle authentication failure gracefully', async () => {
      // Mock MSAL with no accounts
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      const TestComponent = () => {
        const { isAuthenticated } = useAuth();
        return <div data-testid="auth-status">Auth: {isAuthenticated ? 'true' : 'false'}</div>;
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Should show loading initially
      expect(screen.getByText('Loading...')).toBeInTheDocument();

      // Should eventually show that auth is not ready
      await waitFor(() => {
        expect(screen.getByTestId('auth-status')).toHaveTextContent('Auth: false');
      });
    });

    it('should handle token acquisition failure', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);
      (msalInstance.acquireTokenSilent as jest.Mock).mockRejectedValue(new Error('Token acquisition failed'));

      const TestComponent = () => {
        const { isTokenReady, error } = useAuth();
        return (
          <div>
            <div data-testid="token-status">Token: {isTokenReady ? 'true' : 'false'}</div>
            {error && <div data-testid="error">{error.message}</div>}
          </div>
        );
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('token-status')).toHaveTextContent('Token: false');
      });
    });
  });

  describe('State Synchronization', () => {
    it('should synchronize authentication state between MSAL and AuthContext', async () => {
      let isAuthenticatedValue = false;

      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockImplementation(() => isAuthenticatedValue);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      const TestComponent = () => {
        const { isAuthenticated } = useAuth();
        return <div data-testid="auth-state">{isAuthenticated ? 'authenticated' : 'not-authenticated'}</div>;
      };

      const { rerender } = render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Initially not authenticated
      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('not-authenticated');
      });

      // Simulate authentication change
      isAuthenticatedValue = true;

      rerender(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('auth-state')).toHaveTextContent('authenticated');
      });
    });

    it('should handle rapid authentication state changes', async () => {
      let isAuthenticatedValue = false;

      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockImplementation(() => isAuthenticatedValue);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      const stateChanges: string[] = [];

      const TestComponent = () => {
        const { isAuthenticated, isTokenReady } = useAuth();

        React.useEffect(() => {
          stateChanges.push(`auth:${isAuthenticated},token:${isTokenReady}`);
        }, [isAuthenticated, isTokenReady]);

        return <div data-testid="state">State tracked</div>;
      };

      const { rerender } = render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Initial state
      await waitFor(() => {
        expect(stateChanges.length).toBeGreaterThan(0);
      });

      // Simulate rapid changes
      act(() => {
        isAuthenticatedValue = true;
      });

      rerender(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      act(() => {
        isAuthenticatedValue = false;
      });

      rerender(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Should have recorded multiple state changes
      expect(stateChanges.length).toBeGreaterThan(2);
    });
  });

  describe('API Integration with Authentication', () => {
    it('should block API calls until authentication is ready', async () => {
      // Don't signal auth ready initially
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      // API call should timeout/fail
      await expect(apiService.getJobs()).rejects.toThrow('Authentication not ready');

      // Verify fetch was not called
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should allow API calls after authentication is signaled ready', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'test-token',
      });

      const mockApiResponse = { data: 'success' };
      // Reset mocks to ensure we get exactly this response
      mockFetch.mockReset();
      mockFetch.mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue(mockApiResponse),
      } as any);

      // Signal auth ready
      apiService.signalAuthReady();

      const result = await apiService.getJobs();

      expect(result).toEqual(mockApiResponse);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer test-token',
          }),
        })
      );
    });

    it('should handle concurrent API calls correctly', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'test-token',
      });

      const mockApiResponse = { data: 'success' };
      mockFetch.mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue(mockApiResponse),
      } as any);

      // Signal auth ready
      apiService.signalAuthReady();

      // Make concurrent API calls
      const promises = [
        apiService.getJobs(),
        apiService.getCustomers(),
        apiService.getDrivers(),
      ];

      const results = await Promise.all(promises);

      expect(results).toHaveLength(3);
      expect(results[0]).toEqual(mockApiResponse);
      expect(results[1]).toEqual(mockApiResponse);
      expect(results[2]).toEqual(mockApiResponse);

      // Should have made 3 API calls
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });
  });

  describe('Error Recovery', () => {
    it('should recover from token acquisition failures', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);

      // First call fails, second succeeds
      // Note: getAuthToken calls acquireTokenSilent.
      // If it fails, it calls acquireTokenPopup.
      
      // Setup the sequence for acquireTokenSilent
      (msalInstance.acquireTokenSilent as jest.Mock)
        .mockRejectedValueOnce(new Error('Silent token failure'))
        .mockResolvedValueOnce({ accessToken: 'retry-token' });
        
      // Setup fallback to popup if silent fails
      (msalInstance.acquireTokenPopup as jest.Mock)
        .mockResolvedValue({ accessToken: 'popup-token' });

      const mockApiResponse = { data: 'success' };
      mockFetch.mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue(mockApiResponse),
      } as any);

      // Signal auth ready
      apiService.signalAuthReady();

      const result = await apiService.getJobs();

      expect(result).toEqual(mockApiResponse);
      
      // It should try silent, fail, then try popup
      // Or if the test intends to retry silent? 
      // looking at api.ts:
      // try { silent } catch { try { popup } }
      // So acquireTokenSilent is called once, then acquireTokenPopup.
      
      expect(msalInstance.acquireTokenSilent).toHaveBeenCalledTimes(1);
      expect(msalInstance.acquireTokenPopup).toHaveBeenCalledTimes(1);
    });

    it('should handle API errors gracefully', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'test-token',
      });

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        text: jest.fn().mockResolvedValue('Unauthorized'),
      } as any);

      // Signal auth ready
      apiService.signalAuthReady();

      await expect(apiService.getJobs()).rejects.toThrow('API Error: 401 Unauthorized');
    });
  });

  describe('Race Condition Prevention', () => {
    it('should prevent component rendering before authentication is ready', async () => {
      let renderCount = 0;

      const TestComponent = () => {
        renderCount++;
        return <div>Protected Content</div>;
      };

      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.Login,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Should show loading, not the protected component
      expect(screen.getByText('Loading...')).toBeInTheDocument();
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();

      // Component should not have rendered yet
      expect(renderCount).toBe(0);
    });

    it('should handle authentication state changes during API calls', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(mockAccount);
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'test-token',
      });

      // Slow API response
      const mockApiResponse = { data: 'success' };
      mockFetch.mockImplementation(
        () => new Promise(resolve =>
          setTimeout(() => resolve({
            ok: true,
            json: jest.fn().mockResolvedValue(mockApiResponse),
          } as any), 100)
        )
      );

      // Signal auth ready
      apiService.signalAuthReady();

      const apiPromise = apiService.getJobs();

      // Simulate auth state change during API call
      act(() => {
        mockUseIsAuthenticated.mockReturnValue(false);
      });

      const result = await apiPromise;

      // API call should still complete successfully
      expect(result).toEqual(mockApiResponse);
    });
  });
});