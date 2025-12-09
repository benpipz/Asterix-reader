import * as signalR from '@microsoft/signalr';
import { ReceivedData } from '../types/data';

// SignalR Hub URL - adjust if your backend runs on a different port
const HUB_URL = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5000/datahub';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private onDataReceivedCallback: ((data: ReceivedData) => void) | null = null;
  private onStateChangeCallback: ((state: signalR.HubConnectionState) => void) | null = null;

  async start(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    // Stop existing connection if any
    if (this.connection) {
      await this.connection.stop().catch(() => {});
      this.connection = null;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, then 30s intervals
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Set up event handlers
    this.connection.on('DataReceived', (data: ReceivedData) => {
      if (this.onDataReceivedCallback) {
        this.onDataReceivedCallback(data);
      }
    });

    // Set up connection state change handlers
    this.connection.onreconnecting(() => {
      if (this.onStateChangeCallback) {
        this.onStateChangeCallback(signalR.HubConnectionState.Reconnecting);
      }
    });

    this.connection.onreconnected(() => {
      if (this.onStateChangeCallback) {
        this.onStateChangeCallback(signalR.HubConnectionState.Connected);
      }
    });

    this.connection.onclose(() => {
      if (this.onStateChangeCallback) {
        this.onStateChangeCallback(signalR.HubConnectionState.Disconnected);
      }
    });

    try {
      await this.connection.start();
      console.log('SignalR Connected to:', HUB_URL);
      if (this.onStateChangeCallback) {
        this.onStateChangeCallback(signalR.HubConnectionState.Connected);
      }
    } catch (error) {
      console.error('SignalR Connection Error:', error);
      console.error('Attempted to connect to:', HUB_URL);
      console.error('Make sure the backend is running and accessible at this URL');
      
      // Don't throw immediately - let automatic reconnect handle it
      if (this.onStateChangeCallback) {
        this.onStateChangeCallback(signalR.HubConnectionState.Disconnected);
      }
      
      // Only throw if it's a critical error that won't be retried
      if (error instanceof Error && error.message.includes('Failed to start')) {
        // Automatic reconnect will handle retries
        console.warn('Connection failed, automatic reconnect will attempt to restore connection');
      } else {
        throw error;
      }
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  onDataReceived(callback: (data: ReceivedData) => void): void {
    this.onDataReceivedCallback = callback;
  }

  onStateChange(callback: (state: signalR.HubConnectionState) => void): void {
    this.onStateChangeCallback = callback;
  }

  getConnection(): signalR.HubConnection | null {
    return this.connection;
  }

  async getAllData(): Promise<ReceivedData[]> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }
    return await this.connection.invoke<ReceivedData[]>('GetAllData');
  }

  async getLatestData(): Promise<ReceivedData | null> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }
    return await this.connection.invoke<ReceivedData | null>('GetLatestData');
  }

  async getDataCount(): Promise<number> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }
    return await this.connection.invoke<number>('GetDataCount');
  }

  getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  async clearAllData(): Promise<void> {
    const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';
    const response = await fetch(`${API_URL}/api/data`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to clear data: ${response.statusText}`);
    }
  }
}

export const signalRService = new SignalRService();

