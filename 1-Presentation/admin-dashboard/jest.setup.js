import '@testing-library/jest-dom'

// Mock MSAL
jest.mock('@azure/msal-browser', () => ({
  PublicClientApplication: jest.fn().mockImplementation(() => ({
    initialize: jest.fn().mockResolvedValue(undefined),
    getAllAccounts: jest.fn().mockReturnValue([]),
    getActiveAccount: jest.fn().mockReturnValue(null),
    setActiveAccount: jest.fn(),
    acquireTokenSilent: jest.fn().mockResolvedValue({
      accessToken: 'mock-access-token',
      account: { username: 'test@example.com' }
    }),
    acquireTokenPopup: jest.fn().mockResolvedValue({
      accessToken: 'mock-access-token',
      account: { username: 'test@example.com' }
    }),
    loginPopup: jest.fn().mockResolvedValue({
      accessToken: 'mock-access-token',
      account: { username: 'test@example.com' }
    }),
    logout: jest.fn().mockResolvedValue(undefined),
  })),
  InteractionStatus: {
    None: 'none',
    Login: 'login',
    AcquireToken: 'acquireToken',
    SsoSilent: 'ssoSilent',
    HandleRedirect: 'handleRedirect',
  },
  EventType: {
    LOGIN_SUCCESS: 'LOGIN_SUCCESS',
    LOGIN_FAILURE: 'LOGIN_FAILURE',
    LOGOUT_SUCCESS: 'LOGOUT_SUCCESS',
    ACQUIRE_TOKEN_SUCCESS: 'ACQUIRE_TOKEN_SUCCESS',
    ACQUIRE_TOKEN_FAILURE: 'ACQUIRE_TOKEN_FAILURE',
  },
}));

jest.mock('@azure/msal-react', () => ({
  useMsal: jest.fn(),
  useIsAuthenticated: jest.fn(),
  MsalProvider: ({ children }) => children,
  MsalAuthenticationTemplate: ({ children }) => children,
}));

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  usePathname: jest.fn(),
}));

// Mock logger
jest.mock('@/lib/logger', () => ({
  logger: {
    debug: jest.fn(),
    info: jest.fn(),
    warn: jest.fn(),
    error: jest.fn(),
    apiRequest: jest.fn(),
    apiResponse: jest.fn(),
  },
}));

// Mock appInsights
jest.mock('@/lib/appInsights', () => ({
  appInsights: {
    trackException: jest.fn(),
  },
}));

// Mock providers
jest.mock('@/lib/providers', () => ({
  msalInstance: {
    initialize: jest.fn().mockResolvedValue(undefined),
    getAllAccounts: jest.fn().mockReturnValue([]),
    getActiveAccount: jest.fn().mockReturnValue(null),
    setActiveAccount: jest.fn(),
    acquireTokenSilent: jest.fn().mockResolvedValue({
      accessToken: 'mock-access-token',
      account: { username: 'test@example.com' }
    }),
    acquireTokenPopup: jest.fn().mockResolvedValue({
      accessToken: 'mock-access-token',
      account: { username: 'test@example.com' }
    }),
  },
}));

// Mock config
jest.mock('@/config/auth', () => ({
  apiTokenRequest: {
    scopes: ['api://test/.default'],
  },
}));

// Global test utilities
global.fetch = jest.fn();

// Mock environment variables
process.env.NEXT_PUBLIC_AZURE_AD_SCOPES = 'api://test/.default';
process.env.NEXT_PUBLIC_API_BASE_URL = 'http://localhost:3001/api';