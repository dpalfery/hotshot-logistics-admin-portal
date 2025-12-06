'use client';

import { useEffect, useState } from 'react';
import dynamic from 'next/dynamic';
import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';
import { signalRService } from '@/services/signalr';
import { LocationUpdate, NotificationMessage, JobStatus } from '@/types';
import { MapIcon, BellIcon } from '@heroicons/react/24/outline';

// Dynamically import the Map component to avoid SSR issues with Leaflet
const Map = dynamic(() => import('./Map').then(mod => ({ default: mod.Map })), {
  ssr: false,
  loading: () => (
    <div className="bg-gray-100 h-64 rounded-lg flex items-center justify-center">
      <p className="text-gray-500">Loading map...</p>
    </div>
  ),
});

export function TrackingDashboard() {
  const [locationUpdates, setLocationUpdates] = useState<LocationUpdate[]>([]);
  const [notifications, setNotifications] = useState<NotificationMessage[]>([]);

  const { data: jobsResult } = useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiService.getJobs(),
  });

  useEffect(() => {
    // Connect to SignalR
    const connectSignalR = async () => {
      try {
        await signalRService.connect();

        // Set up event handlers
        signalRService.onLocationUpdate((update) => {
          setLocationUpdates(prev => [update, ...prev.slice(0, 9)]); // Keep last 10
        });

        signalRService.onNotification((notification) => {
          setNotifications(prev => [notification, ...prev.slice(0, 9)]); // Keep last 10
        });

      } catch (error) {
        console.error('Failed to connect to SignalR:', error);
      }
    };

    connectSignalR();

    return () => {
      signalRService.disconnect();
    };
  }, []);

  const activeJobs = jobsResult?.items.filter(job =>
    job.status === JobStatus.EnRoute || job.status === JobStatus.Assigned
  ) || [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Real-Time Tracking</h1>
        <p className="text-gray-600">Live maps, job monitoring, and notifications</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Interactive Map with OpenStreetMap */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center mb-4">
            <MapIcon className="h-6 w-6 text-gray-400 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Live Map</h3>
          </div>
          <div className="h-[400px] rounded-lg overflow-hidden">
            <Map 
              activeJobs={activeJobs} 
              locationUpdates={locationUpdates}
            />
          </div>
        </div>

        {/* Active Jobs */}
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Active Jobs</h3>
          <div className="space-y-3">
            {activeJobs.length === 0 ? (
              <p className="text-gray-500">No active jobs</p>
            ) : (
              activeJobs.map((job) => (
                <div key={job.id} className="border border-gray-200 rounded-lg p-3">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">{job.title}</p>
                      <p className="text-sm text-gray-600">
                        {job.pickupAddress} → {job.dropoffAddress}
                      </p>
                      <p className="text-xs text-gray-500">
                        Driver: {job.assignedDriverId ? `Driver #${job.assignedDriverId}` : 'Unassigned'}
                      </p>
                    </div>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      job.status === JobStatus.EnRoute
                        ? 'bg-green-100 text-green-800'
                        : 'bg-blue-100 text-blue-800'
                    }`}>
                      {job.status}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Location Updates */}
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Recent Location Updates</h3>
          <div className="space-y-3 max-h-64 overflow-y-auto">
            {locationUpdates.length === 0 ? (
              <p className="text-gray-500">No recent location updates</p>
            ) : (
              locationUpdates.map((update, index) => (
                <div key={index} className="border border-gray-200 rounded-lg p-3">
                  <p className="text-sm font-medium text-gray-900">
                    Job #{update.jobId} - Driver #{update.driverId}
                  </p>
                  <p className="text-xs text-gray-600">
                    {update.location.latitude.toFixed(4)}, {update.location.longitude.toFixed(4)}
                  </p>
                  <p className="text-xs text-gray-500">
                    {new Date(update.timestamp).toLocaleTimeString()}
                  </p>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Notifications */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center mb-4">
            <BellIcon className="h-6 w-6 text-gray-400 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Notifications</h3>
          </div>
          <div className="space-y-3 max-h-64 overflow-y-auto">
            {notifications.length === 0 ? (
              <p className="text-gray-500">No recent notifications</p>
            ) : (
              notifications.map((notification, index) => (
                <div key={index} className="border border-gray-200 rounded-lg p-3">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{notification.title}</p>
                      <p className="text-sm text-gray-600">{notification.message}</p>
                    </div>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      notification.read
                        ? 'bg-gray-100 text-gray-800'
                        : 'bg-blue-100 text-blue-800'
                    }`}>
                      {notification.read ? 'Read' : 'New'}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">
                    {new Date(notification.timestamp).toLocaleTimeString()}
                  </p>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}