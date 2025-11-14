import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import JobStatusCards from './JobStatusCards';
import { apiService } from '@/services/api';

// Mock the API service
vi.mock('@/services/api', () => ({
  apiService: {
    getJobStatusSummary: vi.fn(),
  },
}));

describe('JobStatusCards', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
        },
      },
    });
    vi.clearAllMocks();
  });

  const renderComponent = () => {
    return render(
      <QueryClientProvider client={queryClient}>
        <JobStatusCards />
      </QueryClientProvider>
    );
  };

  const mockStatusSummary = {
    pendingCount: 5,
    assignedCount: 10,
    enRouteCount: 3,
    receivedCount: 12,
  };

  it('should render loading state initially', () => {
    vi.mocked(apiService.getJobStatusSummary).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    renderComponent();

    // Should show 4 skeleton loaders
    const skeletons = screen.getAllByRole('generic').filter(
      el => el.className.includes('animate-pulse')
    );
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('should render status cards with correct data', async () => {
    vi.mocked(apiService.getJobStatusSummary).mockResolvedValueOnce(mockStatusSummary);

    renderComponent();

    // Wait for data to load
    await waitFor(() => {
      expect(screen.getByTestId('status-card-pending')).toBeInTheDocument();
    });

    // Check all status cards are rendered
    expect(screen.getByTestId('status-card-pending')).toBeInTheDocument();
    expect(screen.getByTestId('status-card-assigned')).toBeInTheDocument();
    expect(screen.getByTestId('status-card-enroute')).toBeInTheDocument();
    expect(screen.getByTestId('status-card-received')).toBeInTheDocument();

    // Check counts are correct
    const pendingCard = screen.getByTestId('status-card-pending');
    expect(pendingCard).toHaveTextContent('5');
    expect(pendingCard).toHaveTextContent('Pending');

    const assignedCard = screen.getByTestId('status-card-assigned');
    expect(assignedCard).toHaveTextContent('10');
    expect(assignedCard).toHaveTextContent('Assigned');

    const enRouteCard = screen.getByTestId('status-card-enroute');
    expect(enRouteCard).toHaveTextContent('3');
    expect(enRouteCard).toHaveTextContent('EnRoute');

    const receivedCard = screen.getByTestId('status-card-received');
    expect(receivedCard).toHaveTextContent('12');
    expect(receivedCard).toHaveTextContent('Received');
  });

  it('should handle zero counts', async () => {
    vi.mocked(apiService.getJobStatusSummary).mockResolvedValueOnce({
      pendingCount: 0,
      assignedCount: 0,
      enRouteCount: 0,
      receivedCount: 0,
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('status-card-pending')).toBeInTheDocument();
    });

    // All counts should be 0
    expect(screen.getByTestId('status-card-pending')).toHaveTextContent('0');
    expect(screen.getByTestId('status-card-assigned')).toHaveTextContent('0');
    expect(screen.getByTestId('status-card-enroute')).toHaveTextContent('0');
    expect(screen.getByTestId('status-card-received')).toHaveTextContent('0');
  });

  it('should render error state on API failure', async () => {
    vi.mocked(apiService.getJobStatusSummary).mockRejectedValueOnce(
      new Error('API Error')
    );

    renderComponent();

    await waitFor(() => {
      expect(
        screen.getByText(/Failed to load job status summary/i)
      ).toBeInTheDocument();
    });

    const errorMessage = screen.getByText(/Failed to load job status summary/i);
    expect(errorMessage).toBeInTheDocument();
    // Check that the error message is contained in an element with red background
    expect(errorMessage).toHaveClass('text-red-600');
  });

  it('should handle undefined counts gracefully', async () => {
    vi.mocked(apiService.getJobStatusSummary).mockResolvedValueOnce({
      pendingCount: undefined as unknown as number,
      assignedCount: undefined as unknown as number,
      enRouteCount: undefined as unknown as number,
      receivedCount: undefined as unknown as number,
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('status-card-pending')).toBeInTheDocument();
    });

    // Should default to 0 when undefined
    expect(screen.getByTestId('status-card-pending')).toHaveTextContent('0');
    expect(screen.getByTestId('status-card-assigned')).toHaveTextContent('0');
    expect(screen.getByTestId('status-card-enroute')).toHaveTextContent('0');
    expect(screen.getByTestId('status-card-received')).toHaveTextContent('0');
  });

  it('should apply correct styling to status cards', async () => {
    vi.mocked(apiService.getJobStatusSummary).mockResolvedValueOnce(mockStatusSummary);

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('status-card-pending')).toBeInTheDocument();
    });

    const pendingCard = screen.getByTestId('status-card-pending');
    expect(pendingCard).toHaveClass('bg-white', 'p-6', 'rounded-lg', 'shadow');
  });

  it('should render icons for each status', async () => {
    vi.mocked(apiService.getJobStatusSummary).mockResolvedValueOnce(mockStatusSummary);

    const { container } = renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('status-card-pending')).toBeInTheDocument();
    });

    // Check that icons are rendered (Lucide icons render as SVG elements)
    const icons = container.querySelectorAll('svg');
    expect(icons.length).toBeGreaterThanOrEqual(4); // At least 4 icons (one per card)
  });
});
