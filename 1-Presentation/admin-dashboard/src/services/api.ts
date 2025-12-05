import { msalInstance } from '@/lib/providers';
import { loginRequest, apiTokenRequest } from '@/config/auth';
import { Job, Driver, Invoice, Customer, PagedResult, JobFilter, PaginationParameters, InvoiceSummaryMetrics, InvoiceAgingBuckets, JobStatusSummary } from '@/types';
import { logger } from '@/lib/logger';

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL || '/api').trim();

class ApiService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    query?: string
  ): Promise<T> {
    const baseUrl = API_BASE_URL.replace(/\/$/, '');
    const normalizedEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    const url = `${baseUrl}${normalizedEndpoint}${query ? `?${query}` : ''}`;

    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    };

    // Add authentication token if available
    const token = await this.getAuthToken();
    const hasToken = !!token;
    if (token) {
      // Use Test scheme for test token, Bearer for real tokens
      const scheme = token === 'test-token' ? 'Test' : 'Bearer';
      config.headers = {
        ...config.headers,
        Authorization: `${scheme} ${token}`,
      };
    }

    // Log API request with sanitized context (never logs actual token or full URL)
    logger.apiRequest(options.method || 'GET', url, {
      hasAuthentication: hasToken,
    });

    try {
      const response = await fetch(url, config);

      if (!response.ok) {
        const error = await response.text();
        logger.error('API request failed', {
          status: response.status,
          endpoint: normalizedEndpoint,
        });
        throw new Error(`API Error: ${response.status} ${error}`);
      }

      const data = await response.json();

      // Log API response with sanitized context
      logger.apiResponse(options.method || 'GET', url, response.status, {
        dataType: Array.isArray(data) ? 'array' : typeof data,
        itemCount: Array.isArray(data) ? data.length : undefined,
      });

      return data;
    } catch (error) {
      logger.error('API fetch error', {
        endpoint: normalizedEndpoint,
        error: error instanceof Error ? error.message : 'Unknown error',
      });
      throw error;
    }
  }

  private async getAuthToken(): Promise<string | null> {
    // Always use test authentication in development
    if (process.env.NODE_ENV === 'development') {
      logger.debug('Using test authentication in development mode');
      return 'test-token';
    }

    const account = msalInstance.getActiveAccount();
    if (!account) {
      logger.debug('No active MSAL account found');
      return null;
    }

    try {
      const response = await msalInstance.acquireTokenSilent({
        ...apiTokenRequest,
        account,
      });
      logger.debug('Token acquired silently');
      return response.accessToken;
    } catch (error) {
      logger.warn('Silent token acquisition failed, attempting interactive', {
        error: error instanceof Error ? error.message : 'Unknown error',
      });
      // Fallback to interactive method if silent acquisition fails
      try {
        const response = await msalInstance.acquireTokenPopup({
          ...apiTokenRequest,
          account,
        });
        logger.debug('Token acquired via popup');
        return response.accessToken;
      } catch (popupError) {
        logger.error('Token acquisition failed', {
          error: popupError instanceof Error ? popupError.message : 'Unknown error',
        });
        return null;
      }
    }
  }

  // Job API methods
  async getJobs(
    filter?: JobFilter,
    pagination?: PaginationParameters
  ): Promise<PagedResult<Job>> {
    const params = new URLSearchParams();

    if (filter) {
      Object.entries(filter).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          params.append(key, value.toString());
        }
      });
    }

    if (pagination) {
      params.append('pageNumber', pagination.pageNumber.toString());
      params.append('pageSize', pagination.pageSize.toString());
    }

    const query = params.toString();
    if (query) {
      return this.request<PagedResult<Job>>('/job', {}, query);
    } else {
      return this.request<PagedResult<Job>>('/job');
    }
  }

  async getJobById(id: string): Promise<Job> {
    return this.request<Job>(`/job/${id}`);
  }

  async createJob(job: Partial<Job>): Promise<Job> {
    return this.request<Job>('/job', {
      method: 'POST',
      body: JSON.stringify(job),
    });
  }

  async updateJob(id: string, job: Partial<Job>): Promise<Job> {
    return this.request<Job>(`/job/${id}`, {
      method: 'PUT',
      body: JSON.stringify(job),
    });
  }

  async deleteJob(id: string): Promise<void> {
    await this.request(`/job/${id}`, {
      method: 'DELETE',
    });
  }

  async assignDriver(jobId: string, driverId: number): Promise<Job> {
    return this.request<Job>(`/job/${jobId}/assign-driver`, {
      method: 'POST',
      body: JSON.stringify({ driverId }),
    });
  }

  async updateJobStatus(jobId: string, status: string): Promise<Job> {
    return this.request<Job>(`/job/${jobId}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    });
  }

  async getJobStatusSummary(): Promise<JobStatusSummary> {
    return this.request<JobStatusSummary>('/job/status-summary');
  }

  // Driver API methods
  async getDrivers(): Promise<Driver[]> {
    return this.request<Driver[]>('/drivers');
  }

  async getDriverById(id: number): Promise<Driver> {
    return this.request<Driver>(`/drivers/${id}`);
  }

  async createDriver(driver: Partial<Driver>): Promise<Driver> {
    return this.request<Driver>('/drivers', {
      method: 'POST',
      body: JSON.stringify(driver),
    });
  }

  async updateDriver(id: number, driver: Partial<Driver>): Promise<Driver> {
    return this.request<Driver>(`/drivers/${id}`, {
      method: 'PUT',
      body: JSON.stringify(driver),
    });
  }

  async deleteDriver(id: number): Promise<void> {
    await this.request(`/drivers/${id}`, {
      method: 'DELETE',
    });
  }

  // Invoice API methods
  async getInvoices(): Promise<PagedResult<Invoice>> {
    return this.request<PagedResult<Invoice>>('/billing/invoices');
  }

  async getOverdueInvoices(): Promise<Invoice[]> {
    return this.request<Invoice[]>('/billing/invoices/overdue');
  }

  async getInvoiceById(id: string): Promise<Invoice> {
    return this.request<Invoice>(`/billing/invoices/${id}`);
  }

  async createInvoice(invoice: Partial<Invoice>): Promise<Invoice> {
    return this.request<Invoice>('/billing/invoices', {
      method: 'POST',
      body: JSON.stringify(invoice),
    });
  }

  async updateInvoice(id: string, invoice: Partial<Invoice>): Promise<Invoice> {
    return this.request<Invoice>(`/billing/invoices/${id}`, {
      method: 'PUT',
      body: JSON.stringify(invoice),
    });
  }

  async deleteInvoice(id: string): Promise<void> {
    await this.request(`/billing/invoices/${id}`, {
      method: 'DELETE',
    });
  }

  async getInvoiceSummary(): Promise<InvoiceSummaryMetrics> {
    return this.request<InvoiceSummaryMetrics>('/invoices/summary');
  }

  async getInvoiceAging(): Promise<InvoiceAgingBuckets> {
    return this.request<InvoiceAgingBuckets>('/invoices/aging');
  }

  // Customer API methods
  async getCustomers(): Promise<Customer[]> {
    return this.request<Customer[]>('/customer');
  }

  async getCustomerById(id: string): Promise<Customer> {
    return this.request<Customer>(`/customer/${id}`);
  }

  async createCustomer(customer: Partial<Customer>): Promise<Customer> {
    return this.request<Customer>('/customer', {
      method: 'POST',
      body: JSON.stringify(customer),
    });
  }

  async updateCustomer(id: string, customer: Partial<Customer>): Promise<Customer> {
    return this.request<Customer>(`/customer/${id}`, {
      method: 'PUT',
      body: JSON.stringify(customer),
    });
  }

  async deleteCustomer(id: string): Promise<void> {
    await this.request(`/customer/${id}`, {
      method: 'DELETE',
    });
  }
}

export const apiService = new ApiService();