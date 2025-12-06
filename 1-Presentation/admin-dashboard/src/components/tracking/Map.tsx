'use client';

import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { Job, LocationUpdate } from '@/types';

// Fix for default marker icons in Leaflet with webpack/next.js
const defaultIcon = L.Icon.Default.prototype as L.Icon & { _getIconUrl?: () => string };
delete defaultIcon._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

interface MapProps {
  activeJobs: Job[];
  locationUpdates: LocationUpdate[];
  className?: string;
}

export function Map({ activeJobs, locationUpdates, className = '' }: MapProps) {
  const mapRef = useRef<L.Map | null>(null);
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const markersRef = useRef<globalThis.Map<string, L.Marker>>(new globalThis.Map());

  // Initialize map
  useEffect(() => {
    if (!mapContainerRef.current || mapRef.current) return;

    // Create map centered on a default location (US center)
    const map = L.map(mapContainerRef.current).setView([39.8283, -98.5795], 4);

    // Add OpenStreetMap tile layer
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19,
    }).addTo(map);

    mapRef.current = map;

    return () => {
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
  }, []);

  // Update markers for active jobs
  useEffect(() => {
    if (!mapRef.current) return;

    const map = mapRef.current;
    const markers = markersRef.current;

    // Clear old markers
    markers.forEach(marker => marker.remove());
    markers.clear();

    // Add markers for jobs with pickup/delivery locations
    const bounds: L.LatLng[] = [];

    activeJobs.forEach(job => {
      if (job.pickupLocation) {
        const pickupLatLng: L.LatLngExpression = [
          job.pickupLocation.latitude,
          job.pickupLocation.longitude
        ];
        
        const pickupMarker = L.marker(pickupLatLng, {
          icon: L.divIcon({
            className: 'custom-marker',
            html: `<div style="background-color: #3b82f6; color: white; border-radius: 50%; width: 30px; height: 30px; display: flex; align-items: center; justify-content: center; font-weight: bold; border: 2px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.3);">P</div>`,
            iconSize: [30, 30],
            iconAnchor: [15, 15],
          })
        }).addTo(map);

        pickupMarker.bindPopup(`
          <div style="min-width: 200px;">
            <strong>${job.title}</strong><br/>
            <em>Pickup Location</em><br/>
            ${job.pickupAddress}
          </div>
        `);

        markers.set(`${job.id}-pickup`, pickupMarker);
        bounds.push(L.latLng(pickupLatLng));
      }

      if (job.deliveryLocation) {
        const deliveryLatLng: L.LatLngExpression = [
          job.deliveryLocation.latitude,
          job.deliveryLocation.longitude
        ];
        
        const deliveryMarker = L.marker(deliveryLatLng, {
          icon: L.divIcon({
            className: 'custom-marker',
            html: `<div style="background-color: #10b981; color: white; border-radius: 50%; width: 30px; height: 30px; display: flex; align-items: center; justify-content: center; font-weight: bold; border: 2px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.3);">D</div>`,
            iconSize: [30, 30],
            iconAnchor: [15, 15],
          })
        }).addTo(map);

        deliveryMarker.bindPopup(`
          <div style="min-width: 200px;">
            <strong>${job.title}</strong><br/>
            <em>Delivery Location</em><br/>
            ${job.dropoffAddress}
          </div>
        `);

        markers.set(`${job.id}-delivery`, deliveryMarker);
        bounds.push(L.latLng(deliveryLatLng));
      }

      // Draw a line between pickup and delivery if both exist
      if (job.pickupLocation && job.deliveryLocation) {
        const routeLine = L.polyline([
          [job.pickupLocation.latitude, job.pickupLocation.longitude],
          [job.deliveryLocation.latitude, job.deliveryLocation.longitude]
        ], {
          color: '#6366f1',
          weight: 2,
          opacity: 0.6,
          dashArray: '5, 10'
        }).addTo(map);

        markers.set(`${job.id}-route`, routeLine as unknown as L.Marker);
      }
    });

    // Add markers for recent location updates (driver positions)
    locationUpdates.forEach((update, index) => {
      const driverLatLng: L.LatLngExpression = [
        update.location.latitude,
        update.location.longitude
      ];
      
      const driverMarker = L.marker(driverLatLng, {
        icon: L.divIcon({
          className: 'custom-marker',
          html: `<div style="background-color: #f59e0b; color: white; border-radius: 50%; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center; font-weight: bold; border: 2px solid white; box-shadow: 0 2px 6px rgba(0,0,0,0.4);">🚚</div>`,
          iconSize: [32, 32],
          iconAnchor: [16, 16],
        })
      }).addTo(map);

      driverMarker.bindPopup(`
        <div style="min-width: 200px;">
          <strong>Driver #${update.driverId}</strong><br/>
          Job: ${update.jobId}<br/>
          Last Update: ${new Date(update.timestamp).toLocaleTimeString()}
        </div>
      `);

      markers.set(`driver-${update.driverId}-${index}`, driverMarker);
      bounds.push(L.latLng(driverLatLng));
    });

    // Fit map bounds to show all markers
    if (bounds.length > 0) {
      const latLngBounds = L.latLngBounds(bounds);
      map.fitBounds(latLngBounds, { padding: [50, 50] });
    }
  }, [activeJobs, locationUpdates]);

  return (
    <div 
      ref={mapContainerRef} 
      className={`w-full h-full min-h-[400px] rounded-lg ${className}`}
      style={{ zIndex: 0 }}
    />
  );
}
