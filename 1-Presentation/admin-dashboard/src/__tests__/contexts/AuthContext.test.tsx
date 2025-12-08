import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { AuthProvider, useAuth } from '@/contexts/AuthContext';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';

// Mock MSAL hooks
const mockUseMsal = useMsal as jest.MockedFunction<typeof useMsal>;
const mockUseIsAuthenticated = useIsAuthenticated as jest.MockedFunction<typeof useIsAuthenticated>;

// Mock MSAL instance
jest.mock('@/lib/providers', () => ({
  msalInstance: {
    getAllAccounts: jest.fn(),
    getActiveAccount: jest.fn(),
    setActiveAccount: jest.fn(),
    acquireTokenSilent: jest.fn(),
    acquireTokenPopup: jest.fn(),
  },
}));

describe('AuthContext', () => {
  const mockAccount = {
    username: 'test@example.com',
    name: 'Test User',
    localAccountId: 'test-id',
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Initial State', () => {
    it('should initialize with default state', () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      const TestComponent = () => {
        const { isMsalReady, isAuthChecked, isAuthenticated, isTokenReady } = useAuth();
        return (
          <div>
            <span data-testid="msal-ready">{isMsalReady.toString()}</span>
            <span data-testid="auth-checked">{isAuthChecked.toString()}</span>
            <span data-testid="authenticated">{isAuthenticated.toString()}</span>
            <span data-testid="token-ready">{isTokenReady.toString()}</span>
          </div>
        );
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      expect(screen.getByTestId('msal-ready')).toHaveTextContent('false');
      expect(screen.getByTestId('auth-checked')).toHaveTextContent('false');
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
      expect(screen.getByTestId('token-ready')).toHaveTextContent('false');
    });
  });

  describe('MSAL Initialization', () => {
    it('should set MSAL as ready when interaction status is None', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      const TestComponent = () => {
        const { isMsalReady, isAuthChecked } = useAuth();
        return (
          <div>
            <span data-testid="msal-ready">{isMsalReady.toString()}</span>
            <span data-testid="auth-checked">{isAuthChecked.toString()}</span>
          </div>
        );
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('msal-ready')).toHaveTextContent('true');
        expect(screen.getByTestId('auth-checked')).toHaveTextContent('true');
      });
    });

    it('should set active account when accounts exist', async () => {
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

      const TestComponent = () => {
        const { isAuthenticated, user } = useAuth();
        return (
          <div>
            <span data-testid="authenticated">{isAuthenticated.toString()}</span>
            <span data-testid="user">{user?.username || 'no-user'}</span>
          </div>
        );
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
        expect(screen.getByTestId('user')).toHaveTextContent('test@example.com');
      });

      expect(msalInstance.setActiveAccount).toHaveBeenCalledWith(mockAccount);
    });
  });

  describe('Token Readiness Check', () => {
    it('should set token as ready when silent acquisition succeeds', async () => {
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

      const TestComponent = () => {
        const { isTokenReady } = useAuth();
        return <span data-testid="token-ready">{isTokenReady.toString()}</span>;
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('token-ready')).toHaveTextContent('true');
      });
    });

    it('should handle token acquisition failure gracefully', async () => {
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
        const { isTokenReady } = useAuth();
        return <span data-testid="token-ready">{isTokenReady.toString()}</span>;
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('token-ready')).toHaveTextContent('false');
      });
    });

    it('should not check token readiness when not authenticated', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      const TestComponent = () => {
        const { isTokenReady, checkTokenReadiness } = useAuth();

        // Call checkTokenReadiness manually
        React.useEffect(() => {
          checkTokenReadiness();
        }, [checkTokenReadiness]);

        return <span data-testid="token-ready">{isTokenReady.toString()}</span>;
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('token-ready')).toHaveTextContent('false');
      });

      expect(msalInstance.acquireTokenSilent).not.toHaveBeenCalled();
    });
  });

  describe('Authentication State Changes', () => {
    it('should update authentication state when MSAL reports changes', async () => {
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
        return <span data-testid="authenticated">{isAuthenticated.toString()}</span>;
      };

      const { rerender } = render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Initially not authenticated
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
      });

      // Simulate authentication change
      isAuthenticatedValue = true;

      rerender(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle initialization errors', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      (msalInstance.getAllAccounts as jest.Mock).mockImplementation(() => {
        throw new Error('MSAL initialization failed');
      });

      const TestComponent = () => {
        const { error } = useAuth();
        return <span data-testid="error">{error?.message || 'no-error'}</span>;
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('error')).toHaveTextContent('MSAL initialization failed');
      });
    });
  });

  describe('resetAuthState', () => {
    it('should reset all auth state to initial values', async () => {
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

      const TestComponent = () => {
        const { resetAuthState, isAuthenticated, isTokenReady } = useAuth();

        return (
          <div>
            <span data-testid="authenticated">{isAuthenticated.toString()}</span>
            <span data-testid="token-ready">{isTokenReady.toString()}</span>
            <button data-testid="reset" onClick={resetAuthState}>Reset</button>
          </div>
        );
      };

      render(
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      );

      // Wait for authentication to be ready
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
        expect(screen.getByTestId('token-ready')).toHaveTextContent('true');
      });

      // Reset state
      screen.getByTestId('reset').click();

      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
        expect(screen.getByTestId('token-ready')).toHaveTextContent('false');
      });
    });
  });

  describe('useAuth Hook', () => {
    it('should throw error when used outside AuthProvider', () => {
      const TestComponent = () => {
        useAuth();
        return <div>Test</div>;
      };

      expect(() => render(<TestComponent />)).toThrow('useAuth must be used within an AuthProvider');
    });
  });
});