import { apiService } from '@/services/api';
import { msalInstance } from '@/lib/providers';

// Mock fetch
const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>;

// Mock MSAL instance
jest.mock('@/lib/providers', () => ({
  msalInstance: {
    getActiveAccount: jest.fn(),
    acquireTokenSilent: jest.fn(),
    acquireTokenPopup: jest.fn(),
  },
}));

describe('ApiService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockFetch.mockReset();
    // Reset auth ready state
    (apiService as any).authReadyResolve = null;
    (apiService as any).authReadyPromise = new Promise((resolve) => {
      (apiService as any).authReadyResolve = resolve;
    });
    // Signal auth ready for most tests
    apiService.signalAuthReady();
  });

  describe('Authentication Readiness', () => {
    it('should wait for authentication readiness before making requests', async () => {
      // Reset auth state for this specific test
      (apiService as any).authReadyResolve = null;
      (apiService as any).authReadyPromise = new Promise((resolve) => {
        (apiService as any).authReadyResolve = resolve;
      });

      const mockResponse = { data: 'test' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue({
        username: 'test@example.com',
      });
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'test-token',
      });

      // Signal auth ready
      apiService.signalAuthReady();

      const result = await apiService.getJobs();

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/job'),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer test-token',
          }),
        })
      );
      expect(result).toEqual(mockResponse);
    });

    it('should timeout when authentication readiness takes too long', async () => {
      // Reset auth state and don't signal ready
      (apiService as any).authReadyResolve = null;
      (apiService as any).authReadyPromise = new Promise((resolve) => {
        (apiService as any).authReadyResolve = resolve;
      });

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue({}),
      } as any);

      // Don't signal auth ready - should timeout
      await expect(apiService.getJobs()).rejects.toThrow('Authentication not ready');
    }, 20000);

    it('should handle authentication readiness timeout gracefully', async () => {
      // Reset auth state
      (apiService as any).authReadyResolve = null;
      (apiService as any).authReadyPromise = new Promise((resolve) => {
        (apiService as any).authReadyResolve = resolve;
      });

      // Create a promise that will timeout
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Authentication readiness timeout')), 15000);
      });

      // Mock the waitForAuthReady to use our timeout promise
      const spy = jest.spyOn(apiService as any, 'waitForAuthReady').mockReturnValue(timeoutPromise);

      try {
        await expect(apiService.getJobs()).rejects.toThrow('Authentication readiness timeout');
      } finally {
        spy.mockRestore();
      }
    }, 20000);
  });

  describe('Token Acquisition', () => {
    beforeEach(() => {
      // Signal auth ready for all tests
      apiService.signalAuthReady();
    });

    it('should acquire token silently when account exists', async () => {
      const mockResponse = { data: 'test' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue({
        username: 'test@example.com',
      });
      (msalInstance.acquireTokenSilent as jest.Mock).mockResolvedValue({
        accessToken: 'silent-token',
      });

      await apiService.getJobs();

      expect(msalInstance.acquireTokenSilent).toHaveBeenCalled();
      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer silent-token',
          }),
        })
      );
    }, 10000);

    it('should fallback to popup when silent acquisition fails', async () => {
      const mockResponse = { data: 'test' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue({
        username: 'test@example.com',
      });
      (msalInstance.acquireTokenSilent as jest.Mock).mockRejectedValue(new Error('Silent failed'));
      (msalInstance.acquireTokenPopup as jest.Mock).mockResolvedValue({
        accessToken: 'popup-token',
      });

      await apiService.getJobs();

      expect(msalInstance.acquireTokenSilent).toHaveBeenCalled();
      expect(msalInstance.acquireTokenPopup).toHaveBeenCalled();
      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer popup-token',
          }),
        })
      );
    });

    it('should throw error when both token acquisition methods fail', async () => {
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue({
        username: 'test@example.com',
      });
      (msalInstance.acquireTokenSilent as jest.Mock).mockRejectedValue(new Error('Silent failed'));
      (msalInstance.acquireTokenPopup as jest.Mock).mockRejectedValue(new Error('Popup failed'));

      await expect(apiService.getJobs()).rejects.toThrow('Token acquisition failed');
    });

    it('should not include authorization header when no account exists', async () => {
      const mockResponse = { data: 'test' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await apiService.getJobs();

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.not.objectContaining({
            Authorization: expect.any(String),
          }),
        })
      );
    });
  });

  describe('Request Handling', () => {
    beforeEach(() => {
      apiService.signalAuthReady();
    });

    it('should handle successful API responses', async () => {
      const mockResponse = { id: 1, name: 'Test Job' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      const result = await apiService.getJobById('123');

      expect(result).toEqual(mockResponse);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/job/123'),
        expect.any(Object)
      );
    });

    it('should handle API errors', async () => {
      const errorMessage = 'Not Found';
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        text: jest.fn().mockResolvedValue(errorMessage),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await expect(apiService.getJobById('123')).rejects.toThrow(`API Error: 404 ${errorMessage}`);
    });

    it('should handle network errors', async () => {
      const networkError = new Error('Network failure');
      mockFetch.mockRejectedValueOnce(networkError);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await expect(apiService.getJobById('123')).rejects.toThrow('Network failure');
    });

    it('should build correct URLs with query parameters', async () => {
      const mockResponse = { data: [] };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await apiService.getJobs(
        { status: 'active' },
        { pageNumber: 2, pageSize: 10 }
      );

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/job?status=active&pageNumber=2&pageSize=10'),
        expect.any(Object)
      );
    });

    it('should set correct headers', async () => {
      const mockResponse = { data: 'test' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await apiService.getJobs();

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      );
    });
  });

  describe('CRUD Operations', () => {
    beforeEach(() => {
      apiService.signalAuthReady();
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);
    });

    describe('Job Operations', () => {
      it('should create job with POST request', async () => {
        const jobData = { title: 'New Job', description: 'Test job' };
        const mockResponse = { id: '123', ...jobData };
        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: jest.fn().mockResolvedValue(mockResponse),
        } as any);

        const result = await apiService.createJob(jobData);

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/job'),
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify(jobData),
          })
        );
        expect(result).toEqual(mockResponse);
      });

      it('should update job with PUT request', async () => {
        const jobData = { title: 'Updated Job' };
        const mockResponse = { id: '123', ...jobData };
        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: jest.fn().mockResolvedValue(mockResponse),
        } as any);

        const result = await apiService.updateJob('123', jobData);

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/job/123'),
          expect.objectContaining({
            method: 'PUT',
            body: JSON.stringify(jobData),
          })
        );
        expect(result).toEqual(mockResponse);
      });

      it('should delete job with DELETE request', async () => {
        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: jest.fn().mockResolvedValue({}),
        } as any);

        await apiService.deleteJob('123');

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/job/123'),
          expect.objectContaining({
            method: 'DELETE',
          })
        );
      });
    });

    describe('Customer Operations', () => {
      it('should get customers', async () => {
        const mockResponse = [{ id: '1', name: 'Customer 1' }];
        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: jest.fn().mockResolvedValue(mockResponse),
        } as any);

        const result = await apiService.getCustomers();

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/customer'),
          expect.any(Object)
        );
        expect(result).toEqual(mockResponse);
      });

      it('should create customer', async () => {
        const customerData = { name: 'New Customer', email: 'test@example.com' };
        const mockResponse = { id: '456', ...customerData };
        mockFetch.mockResolvedValueOnce({
          ok: true,
          json: jest.fn().mockResolvedValue(mockResponse),
        } as any);

        const result = await apiService.createCustomer(customerData);

        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/customer'),
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify(customerData),
          })
        );
        expect(result).toEqual(mockResponse);
      });
    });
  });

  describe('Error Propagation', () => {
    beforeEach(() => {
      apiService.signalAuthReady();
    });

    it('should propagate authentication errors', async () => {
      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue({
        username: 'test@example.com',
      });
      (msalInstance.acquireTokenSilent as jest.Mock).mockRejectedValue(new Error('Auth failed'));

      await expect(apiService.getJobs()).rejects.toThrow('Token acquisition failed');
    });

    it('should propagate API errors with status codes', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        text: jest.fn().mockResolvedValue('Internal Server Error'),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await expect(apiService.getJobs()).rejects.toThrow('API Error: 500 Internal Server Error');
    });

    it('should handle malformed JSON responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: jest.fn().mockRejectedValue(new Error('Invalid JSON')),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      await expect(apiService.getJobs()).rejects.toThrow('Invalid JSON');
    });
  });

  describe('Race Condition Prevention', () => {
    it('should handle concurrent API calls correctly', async () => {
      apiService.signalAuthReady();

      const mockResponse = { data: 'test' };
      mockFetch.mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue(mockResponse),
      } as any);

      (msalInstance.getActiveAccount as jest.Mock).mockReturnValue(null);

      // Make multiple concurrent calls
      const promises = [
        apiService.getJobs(),
        apiService.getCustomers(),
        apiService.getDrivers(),
      ];

      const results = await Promise.all(promises);

      // All should succeed
      expect(results).toHaveLength(3);
      expect(results[0]).toEqual(mockResponse);
      expect(results[1]).toEqual(mockResponse);
      expect(results[2]).toEqual(mockResponse);

      // fetch should be called 3 times
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    it('should prevent API calls before auth is ready', async () => {
      // Mock waitForAuthReady to throw
      const spy = jest.spyOn(apiService as any, 'waitForAuthReady').mockRejectedValue(new Error('Authentication not ready'));

      await expect(apiService.getJobs()).rejects.toThrow('Authentication not ready');
      
      expect(mockFetch).not.toHaveBeenCalled();
      
      spy.mockRestore();
    });
  });
});