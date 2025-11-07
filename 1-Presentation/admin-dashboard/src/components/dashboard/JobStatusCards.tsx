'use client';

import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';
import { Hourglass, ClipboardList, Truck, Inbox } from 'lucide-react';

interface StatusCard {
  label: string;
  count: number;
  color: string;
  icon: React.ReactNode;
}

export default function JobStatusCards() {
  const { data: statusSummary, isLoading, error } = useQuery({
    queryKey: ['jobStatusSummary'],
    queryFn: () => apiService.getJobStatusSummary(),
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="bg-white p-6 rounded-lg shadow animate-pulse">
            <div className="h-16 bg-gray-200 rounded"></div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-600">
        Failed to load job status summary. Please try again later.
      </div>
    );
  }

  const statusCards: StatusCard[] = [
    {
      label: 'Pending',
      count: statusSummary?.pendingCount || 0,
      color: '#3B82F6',
      icon: <Hourglass className="w-5 h-5" />,
    },
    {
      label: 'Assigned',
      count: statusSummary?.assignedCount || 0,
      color: '#0EA5E9',
      icon: <ClipboardList className="w-5 h-5" />,
    },
    {
      label: 'EnRoute',
      count: statusSummary?.enRouteCount || 0,
      color: '#0284C7',
      icon: <Truck className="w-5 h-5" />,
    },
    {
      label: 'Received',
      count: statusSummary?.receivedCount || 0,
      color: '#0369A1',
      icon: <Inbox className="w-5 h-5" />,
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
      {statusCards.map((card) => (
        <div
          key={card.label}
          className="bg-white p-6 rounded-lg shadow hover:shadow-md transition-shadow"
          data-testid={`status-card-${card.label.toLowerCase()}`}
        >
          <div className="flex flex-col items-center text-center">
            <div className="flex items-center gap-3 mb-3">
              <div
                className="w-10 h-10 rounded-lg flex items-center justify-center text-white"
                style={{ backgroundColor: card.color }}
              >
                {card.icon}
              </div>
              <span className="text-sm font-semibold text-gray-600">
                {card.label}
              </span>
            </div>
            <div className="text-3xl font-bold text-gray-900">{card.count}</div>
          </div>
        </div>
      ))}
    </div>
  );
}
