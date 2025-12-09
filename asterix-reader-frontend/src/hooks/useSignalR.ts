import { useEffect, useState, useCallback } from 'react';
import { signalRService } from '../services/signalRService';
import { ReceivedData } from '../types/data';
import * as signalR from '@microsoft/signalr';

export const useSignalR = () => {
  const [data, setData] = useState<ReceivedData[]>([]);
  const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const initializeConnection = async () => {
      // Set up real-time data handler first (before connection)
      signalRService.onDataReceived((newData: ReceivedData) => {
        if (mounted) {
          setData((prev) => [newData, ...prev]);
        }
      });

      // Listen for connection state changes
      signalRService.onStateChange((state) => {
        if (mounted) {
          setConnectionState(state);
          setError(null); // Clear error on state change
          // Reload data on reconnect
          if (state === signalR.HubConnectionState.Connected) {
            signalRService.getAllData().then((allData) => {
              if (mounted) setData(allData);
            }).catch((err) => {
              console.error('Error reloading data:', err);
              if (mounted) {
                setError('Failed to load data');
              }
            });
          }
        }
      });

      try {
        await signalRService.start();
        if (!mounted) return;

        setConnectionState(signalRService.getConnectionState());

        // Load all existing data once connected
        try {
          const allData = await signalRService.getAllData();
          if (mounted) {
            setData(allData);
          }
        } catch (err) {
          console.warn('Failed to load initial data, will retry on next connection:', err);
          // Don't set error here - automatic reconnect will handle it
        }
      } catch (err) {
        // Connection failed, but automatic reconnect will handle retries
        if (mounted) {
          const errorMessage = err instanceof Error ? err.message : 'Failed to connect';
          // Only show error if it's not a negotiation error (those will be retried)
          if (!errorMessage.includes('negotiation') && !errorMessage.includes('stopped during')) {
            setError(errorMessage);
          }
          setConnectionState(signalRService.getConnectionState());
        }
      }
    };

    initializeConnection();

    return () => {
      mounted = false;
      signalRService.stop();
    };
  }, []);

  const clearData = useCallback(async () => {
    try {
      await signalRService.clearAllData();
      setData([]);
    } catch (err) {
      console.error('Failed to clear data:', err);
      throw err;
    }
  }, []);

  return {
    data,
    connectionState,
    error,
    clearData,
  };
};

