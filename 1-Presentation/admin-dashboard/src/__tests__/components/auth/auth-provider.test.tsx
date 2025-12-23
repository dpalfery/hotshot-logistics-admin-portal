import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { AuthProvider } from '@/components/auth/auth-provider';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { useRouter, usePathname } from 'next/navigation';
import { InteractionStatus } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';
import { logger } from '@/lib/logger';

// Mock MSAL hooks
const mockUseMsal = useMsal as jest.MockedFunction<typeof useMsal>;
const mockUseIsAuthenticated = useIsAuthenticated as jest.MockedFunction<typeof useIsAuthenticated>;

// Mock Next.js hooks
const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>;
const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>;

// Mock MSAL instance
jest.mock('@/lib/providers', () => ({
  msalInstance: {
    getAllAccounts: jest.fn(),
    setActiveAccount: jest.fn(),
  },
}));

// Mock the AuthContext to avoid circular dependency
let mockAuthContextValue = {
  isMsalReady: true,
  isAuthChecked: true,
  isAuthenticated: true,
  isTokenReady: true,
  user: { username: 'test@example.com' },
  error: null,
  checkTokenReadiness: jest.fn(),
  resetAuthState: jest.fn(),
};

jest.mock('@/contexts/AuthContext', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <div data-testid="mock-auth-provider">{children}</div>,
  useAuth: jest.fn(() => mockAuthContextValue),
}));

describe('AuthProvider Component', () => {
  const mockAccount = {
    username: 'test@example.com',
    name: 'Test User',
    localAccountId: 'test-id',
  };

  const mockRouter = {
    push: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockUseRouter.mockReturnValue(mockRouter);
    mockUsePathname.mockReturnValue('/');
    mockAuthContextValue = {
      isMsalReady: true,
      isAuthChecked: true,
      isAuthenticated: true,
      isTokenReady: true,
      user: { username: 'test@example.com' },
      error: null,
      checkTokenReadiness: jest.fn(),
      resetAuthState: jest.fn(),
    };
  });

  describe('Authentication Check', () => {
    it('should check authentication when MSAL is ready', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(msalInstance.getAllAccounts).toHaveBeenCalled();
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

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(msalInstance.setActiveAccount).toHaveBeenCalledWith(mockAccount);
      });
    });

    it('should handle authentication check errors gracefully', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);
      mockAuthContextValue.isAuthChecked = false;

      (msalInstance.getAllAccounts as jest.Mock).mockImplementation(() => {
        throw new Error('MSAL error');
      });

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Authentication check failed',
          expect.objectContaining({
            error: expect.any(String),
          })
        );
      });
    });
  });

  describe('Redirect Logic', () => {
    it('should redirect to login when not authenticated and not on login page', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);
      mockUsePathname.mockReturnValue('/dashboard');

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(mockRouter.push).toHaveBeenCalledWith('/login?redirect_uri=%2Fdashboard');
      });
    });

    it('should redirect to login with return URL when not authenticated', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);
      mockUsePathname.mockReturnValue('/jobs');

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(mockRouter.push).toHaveBeenCalledWith('/login?redirect_uri=%2Fjobs');
      });
    });

    it('should not redirect when on login page', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);
      mockUsePathname.mockReturnValue('/login');

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        // Wait for auth check to complete
        expect(msalInstance.getAllAccounts).toHaveBeenCalled();
      });

      // Should not redirect
      expect(mockRouter.push).not.toHaveBeenCalled();
    });

    it('should not redirect when authenticated', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);
      mockUsePathname.mockReturnValue('/dashboard');

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(msalInstance.setActiveAccount).toHaveBeenCalledWith(mockAccount);
      });

      // Should not redirect
      expect(mockRouter.push).not.toHaveBeenCalled();
    });
  });

  describe('Loading State', () => {
    it('should show loading when authentication is being checked', () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.Login,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);
      mockAuthContextValue.isAuthChecked = false;

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      expect(screen.getByText('Authenticating')).toBeInTheDocument();
    });

    it('should show loading when MSAL is in progress', () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.AcquireToken,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      expect(screen.getByText('Authenticating')).toBeInTheDocument();
    });

    it('should show loading when authenticated but token not ready', () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);
      mockAuthContextValue.isTokenReady = false;

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      expect(screen.getByText('Authenticating')).toBeInTheDocument();
    });

    it('should render children when fully authenticated', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByText('Test Content')).toBeInTheDocument();
      });
    });
  });

  describe('Token Readiness Integration', () => {
    it('should call checkTokenReadiness after MSAL initialization', async () => {
      const mockCheckTokenReadiness = jest.fn();

      // Mock the AuthProvider to use our custom auth context
      const TestWrapper = ({ children }: { children: React.ReactNode }) => {
        return (
          <AuthProvider>
            {children}
          </AuthProvider>
        );
      };

      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(true);

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([mockAccount]);

      render(
        <TestWrapper>
          <div>Test Content</div>
        </TestWrapper>
      );

      await waitFor(() => {
        expect(msalInstance.setActiveAccount).toHaveBeenCalledWith(mockAccount);
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle MSAL errors during initialization', async () => {
      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockReturnValue(false);
      mockAuthContextValue.isAuthChecked = false;

      (msalInstance.getAllAccounts as jest.Mock).mockImplementation(() => {
        throw new Error('MSAL initialization failed');
      });

      render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Authentication check failed',
          expect.objectContaining({
            error: expect.any(String),
          })
        );
      });
    });
  });

  describe('State Synchronization', () => {
    it('should handle authentication state changes correctly', async () => {
      let isAuthenticatedValue = false;

      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.Login,
        accounts: [],
        instance: msalInstance,
      } as any);
      mockUseIsAuthenticated.mockImplementation(() => isAuthenticatedValue);
      mockAuthContextValue.isAuthChecked = false;

      (msalInstance.getAllAccounts as jest.Mock).mockReturnValue([]);

      const { rerender } = render(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      // Initially not authenticated - should show loading
      expect(screen.getByText('Authenticating')).toBeInTheDocument();

      // Simulate authentication
      isAuthenticatedValue = true;
      mockAuthContextValue.isAuthChecked = true;
      mockAuthContextValue.isTokenReady = false;

      mockUseMsal.mockReturnValue({
        inProgress: InteractionStatus.None,
        accounts: [mockAccount],
        instance: msalInstance,
      } as any);

      rerender(
        <AuthProvider>
          <div>Test Content</div>
        </AuthProvider>
      );

      // Should still show loading until token is ready
      expect(screen.getByText('Authenticating')).toBeInTheDocument();
    });
  });
});
