import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { LocationUpdate, NotificationMessage } from '@/types';

const SIGNALR_URL = process.env.NEXT_PUBLIC_SIGNALR_URL || 'http://localhost:7071/realtime';

// Interface for test mocking
interface MockSignalRService {
  connect: () => Promise<void>;
  disconnect: () => void;
  onLocationUpdate: (callback: (update: LocationUpdate) => void) => void;
  onNotification: (callback: (notification: NotificationMessage) => void) => void;
  onJobStatusUpdate: (callback: (jobId: string, status: string) => void) => void;
  onDriverStatusUpdate: (callback: (driverId: number, isAvailable: boolean) => void) => void;
}

declare global {
  interface Window {
    mockSignalRService?: MockSignalRService;
  }
}

class SignalRService {
  private connection: HubConnection | null = null;
  private isConnected = false;

  async connect(): Promise<void> {
    // Check for mock service in test environment
    if (typeof window !== 'undefined' && window.mockSignalRService) {
      await window.mockSignalRService.connect();
      this.isConnected = true;
      return;
    }

    if (this.connection) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(SIGNALR_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    try {
      await this.connection.start();
      this.isConnected = true;
      console.log('SignalR connected');
    } catch (error) {
      console.error('SignalR connection failed:', error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (typeof window !== 'undefined' && window.mockSignalRService) {
      window.mockSignalRService.disconnect();
      this.isConnected = false;
      return;
    }

    if (this.connection) {
      await this.connection.stop();
      this.isConnected = false;
      this.connection = null;
    }
  }

  // Location tracking methods
  onLocationUpdate(callback: (update: LocationUpdate) => void): void {
    if (typeof window !== 'undefined' && window.mockSignalRService) {
      window.mockSignalRService.onLocationUpdate(callback);
      return;
    }

    if (this.connection) {
      this.connection.on('LocationUpdate', callback);
    }
  }

  offLocationUpdate(callback: (update: LocationUpdate) => void): void {
    if (this.connection) {
      this.connection.off('LocationUpdate', callback);
    }
  }

  // Job status updates
  onJobStatusUpdate(callback: (jobId: string, status: string) => void): void {
    if (typeof window !== 'undefined' && window.mockSignalRService && window.mockSignalRService.onJobStatusUpdate) {
      window.mockSignalRService.onJobStatusUpdate(callback);
      return;
    }

    if (this.connection) {
      this.connection.on('JobStatusUpdate', callback);
    }
  }

  offJobStatusUpdate(callback: (jobId: string, status: string) => void): void {
    if (this.connection) {
      this.connection.off('JobStatusUpdate', callback);
    }
  }

  // Notifications
  onNotification(callback: (notification: NotificationMessage) => void): void {
    if (typeof window !== 'undefined' && window.mockSignalRService) {
      window.mockSignalRService.onNotification(callback);
      return;
    }

    if (this.connection) {
      this.connection.on('Notification', callback);
    }
  }

  offNotification(callback: (notification: NotificationMessage) => void): void {
    if (this.connection) {
      this.connection.off('Notification', callback);
    }
  }

  // Driver availability updates
  onDriverStatusUpdate(callback: (driverId: number, isAvailable: boolean) => void): void {
    if (typeof window !== 'undefined' && window.mockSignalRService && window.mockSignalRService.onDriverStatusUpdate) {
      window.mockSignalRService.onDriverStatusUpdate(callback);
      return;
    }

    if (this.connection) {
      this.connection.on('DriverStatusUpdate', callback);
    }
  }

  offDriverStatusUpdate(callback: (driverId: number, isAvailable: boolean) => void): void {
    if (this.connection) {
      this.connection.off('DriverStatusUpdate', callback);
    }
  }

  // Send location update (for testing or manual updates)
  async sendLocationUpdate(update: LocationUpdate): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendLocationUpdate', update);
    }
  }

  // Join/Leave groups for specific job tracking
  async joinJobGroup(jobId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('JoinJobGroup', jobId);
    }
  }

  async leaveJobGroup(jobId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('LeaveJobGroup', jobId);
    }
  }

  get isConnectionActive(): boolean {
    return this.isConnected;
  }
}

export const signalRService = new SignalRService();