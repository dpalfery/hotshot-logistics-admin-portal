'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { useMsal } from '@azure/msal-react';
import { apiService } from '@/services/api';
import { useAuth } from '@/contexts/AuthContext';
import JobStatusCards from './JobStatusCards';

export function DashboardOverview() {
  const { instance } = useMsal();
  const { isTokenReady, isAuthenticated } = useAuth();
  const activeAccount = instance.getActiveAccount();
  const userName = activeAccount?.name || 'User';
  const userEmail = activeAccount?.username || '';
  const userRoles = (activeAccount?.idTokenClaims as any)?.roles || [];
  const userRole = userRoles.length > 0 ? userRoles[0] : 'User';

  const { data: jobs, isLoading: jobsLoading } = useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiService.getJobs(),
    retry: false,
    enabled: isAuthenticated && isTokenReady, // Only run when auth is ready
  });

  const { data: drivers, isLoading: driversLoading } = useQuery({
    queryKey: ['drivers'],
    queryFn: () => apiService.getDrivers(),
    retry: false,
    enabled: isAuthenticated && isTokenReady, // Only run when auth is ready
  });

  const { data: invoices, isLoading: invoicesLoading } = useQuery({
    queryKey: ['invoices'],
    queryFn: () => apiService.getInvoices(),
    retry: false,
    enabled: isAuthenticated && isTokenReady, // Only run when auth is ready
  });

  const { data: overdueInvoicesData, isLoading: overdueLoading } = useQuery({
    queryKey: ['overdueInvoices'],
    queryFn: () => apiService.getOverdueInvoices(),
    retry: false,
    enabled: isAuthenticated && isTokenReady, // Only run when auth is ready
  });

  const stats = {
    totalJobs: jobs?.totalCount || 0,
    // Active jobs are: Pending (0), Assigned (1), EnRoute (2) - but NOT Received (3)
    activeJobs: jobs?.items.filter(job => job.status !== 3).length || 0,
    pendingJobs: jobs?.items.filter(job => job.status === 0).length || 0,
    totalDrivers: drivers?.length || 0,
    activeDrivers: drivers?.filter(driver => driver.isActive).length || 0,
    totalInvoices: invoices?.totalCount || 0,
    // Use the dedicated overdue invoices endpoint count
    overdueInvoices: overdueInvoicesData?.length || 0,
  };

  if (jobsLoading || driversLoading || invoicesLoading || overdueLoading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/4 mb-4"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="bg-white p-6 rounded-lg shadow">
                <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
                <div className="h-8 bg-gray-200 rounded w-1/2"></div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard Overview</h1>
        <p className="text-gray-600">Welcome to Hotshot Logistics Admin Dashboard</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6" data-testid="stats-container">
        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-blue-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">J</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Total Jobs</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-total-jobs">{stats.totalJobs}</dd>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-green-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">A</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Active Jobs</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-active-jobs">{stats.activeJobs}</dd>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-yellow-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">D</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Active Drivers</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-active-drivers">{stats.activeDrivers}</dd>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-red-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">O</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Overdue Invoices</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-overdue-invoices">{stats.overdueInvoices}</dd>
            </div>
          </div>
        </div>
      </div>

      {/* Job Status Cards */}
      <JobStatusCards />

      {/* Recent Activity */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <h3 className="text-lg leading-6 font-medium text-gray-900">Recent Jobs</h3>
          <div className="mt-5">
            <div className="space-y-3">
              <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                <div>
                  <p className="text-sm font-medium text-gray-900">Urgent Delivery</p>
                  <p className="text-sm text-gray-500">123 Main St → 456 Oak Ave</p>
                </div>
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  InProgress
                </span>
              </div>
              <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                <div>
                  <p className="text-sm font-medium text-gray-900">Standard Delivery</p>
                  <p className="text-sm text-gray-500">789 Pine St → 321 Elm Ave</p>
                </div>
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                  Pending
                </span>
              </div>
            </div>
            <div className="mt-4">
              <a href="/jobs" className="text-sm text-blue-600 hover:text-blue-500">
                View All Jobs →
              </a>
            </div>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <h3 className="text-lg leading-6 font-medium text-gray-900">Quick Actions</h3>
          <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Link href="/jobs?action=create" className="inline-flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
              Create New Job
            </Link>
            <Link href="/drivers" className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
              Add Driver
            </Link>
            <Link href="/billing" className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
              Generate Invoice
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}