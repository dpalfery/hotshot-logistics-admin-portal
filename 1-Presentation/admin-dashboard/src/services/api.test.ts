import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiService } from './api';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

describe('ApiService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset environment to test mode
    process.env.NODE_ENV = 'test';
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Authentication', () => {
    it('should use test-token in development mode', async () => {
      process.env.NODE_ENV = 'development';

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ data: 'test' }),
      });

      await apiService.getDrivers();

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Test test-token',
          }),
        })
      );
    });
  });

  describe('Job API', () => {
    const mockJob = {
      jobId: 'job-123',
      customerId: 'cust-001',
      status: 'Pending',
      pickupLocation: '123 Main St',
      deliveryLocation: '456 Oak Ave',
    };

    it('should get jobs with pagination', async () => {
      const mockResponse = {
        items: [mockJob],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      });

      const result = await apiService.getJobs(
        undefined,
        { pageNumber: 1, pageSize: 10 }
      );

      expect(result).toEqual(mockResponse);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('pageNumber=1'),
        expect.any(Object)
      );
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('pageSize=10'),
        expect.any(Object)
      );
    });

    it('should get jobs with filter', async () => {
      const mockResponse = {
        items: [mockJob],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      });

      await apiService.getJobs(
        { status: 'Pending', customerId: 'cust-001' },
        undefined
      );

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('status=Pending'),
        expect.any(Object)
      );
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('customerId=cust-001'),
        expect.any(Object)
      );
    });

    it('should create a job', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockJob,
      });

      const newJob = {
        customerId: 'cust-001',
        pickupLocation: '123 Main St',
        deliveryLocation: '456 Oak Ave',
      };

      const result = await apiService.createJob(newJob);

      expect(result).toEqual(mockJob);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/job'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(newJob),
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      );
    });

    it('should update a job', async () => {
      const updatedJob = { ...mockJob, status: 'Assigned' };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => updatedJob,
      });

      const result = await apiService.updateJob('job-123', { status: 'Assigned' });

      expect(result).toEqual(updatedJob);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/job/job-123'),
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ status: 'Assigned' }),
        })
      );
    });

    it('should delete a job', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });

      await apiService.deleteJob('job-123');

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/job/job-123'),
        expect.objectContaining({
          method: 'DELETE',
        })
      );
    });

    it('should assign a driver to a job', async () => {
      const assignedJob = { ...mockJob, driverId: 1, status: 'Assigned' };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => assignedJob,
      });

      const result = await apiService.assignDriver('job-123', 1);

      expect(result).toEqual(assignedJob);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/job/job-123/assign-driver'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ driverId: 1 }),
        })
      );
    });
  });

  describe('Driver API', () => {
    const mockDriver = {
      driverId: 1,
      name: 'John Doe',
      phoneNumber: '555-1234',
      vehicleType: 'Van',
      status: 'Available',
    };

    it('should get all drivers', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [mockDriver],
      });

      const result = await apiService.getDrivers();

      expect(result).toEqual([mockDriver]);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/drivers'),
        expect.any(Object)
      );
    });

    it('should get driver by id', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockDriver,
      });

      const result = await apiService.getDriverById(1);

      expect(result).toEqual(mockDriver);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/drivers/1'),
        expect.any(Object)
      );
    });

    it('should create a driver', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockDriver,
      });

      const newDriver = {
        name: 'John Doe',
        phoneNumber: '555-1234',
        vehicleType: 'Van',
      };

      const result = await apiService.createDriver(newDriver);

      expect(result).toEqual(mockDriver);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/drivers'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(newDriver),
        })
      );
    });

    it('should update a driver', async () => {
      const updatedDriver = { ...mockDriver, status: 'Unavailable' };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => updatedDriver,
      });

      const result = await apiService.updateDriver(1, { status: 'Unavailable' });

      expect(result).toEqual(updatedDriver);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/drivers/1'),
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ status: 'Unavailable' }),
        })
      );
    });

    it('should delete a driver', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });

      await apiService.deleteDriver(1);

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/drivers/1'),
        expect.objectContaining({
          method: 'DELETE',
        })
      );
    });
  });

  describe('Customer API', () => {
    const mockCustomer = {
      id: 'cust-001',
      name: 'Test Company',
      email: 'test@company.com',
      phone: '555-5678',
    };

    it('should get all customers', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [mockCustomer],
      });

      const result = await apiService.getCustomers();

      expect(result).toEqual([mockCustomer]);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/customer'),
        expect.any(Object)
      );
    });

    it('should get customer by id', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockCustomer,
      });

      const result = await apiService.getCustomerById('cust-001');

      expect(result).toEqual(mockCustomer);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/customer/cust-001'),
        expect.any(Object)
      );
    });
  });

  describe('Invoice API', () => {
    const mockInvoice = {
      id: 'inv-001',
      invoiceNumber: 'INV-001',
      amount: 1500.00,
      status: 'Sent',
    };

    it('should get invoices', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [mockInvoice],
      });

      const result = await apiService.getInvoices();

      expect(result.items).toEqual([mockInvoice]);
      expect(result.totalCount).toBe(1);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/billing/invoices/overdue'),
        expect.any(Object)
      );
    });

    it('should get invoice by id', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockInvoice,
      });

      const result = await apiService.getInvoiceById('inv-001');

      expect(result).toEqual(mockInvoice);
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/billing/invoices/inv-001'),
        expect.any(Object)
      );
    });
  });

  describe('Error Handling', () => {
    it('should throw error on non-ok response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        text: async () => 'Not Found',
      });

      await expect(apiService.getDrivers()).rejects.toThrow('API Error: 404 Not Found');
    });

    it('should throw error on network failure', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'));

      await expect(apiService.getDrivers()).rejects.toThrow('Network error');
    });
  });

  describe('URL Construction', () => {
    it('should build URL correctly with base URL', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      });

      await apiService.getDrivers();

      const calledUrl = mockFetch.mock.calls[0][0];
      expect(calledUrl).toContain('/api/drivers');
    });

    it('should handle trailing slashes in base URL', async () => {
      const originalEnv = process.env.NEXT_PUBLIC_API_BASE_URL;
      process.env.NEXT_PUBLIC_API_BASE_URL = 'http://localhost:7060/api/';

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      });

      await apiService.getDrivers();

      const calledUrl = mockFetch.mock.calls[0][0];
      // Should not have double slashes
      expect(calledUrl).not.toContain('//drivers');

      process.env.NEXT_PUBLIC_API_BASE_URL = originalEnv;
    });
  });
});
