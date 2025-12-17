import { UdpReceiverConfig, PcapReceiverConfig, ReceiverStatus } from '../types/receiver';

const API_URL = import.meta.env.VITE_API_URL || '';

class ReceiverService {
  async startUdpReceiver(config: UdpReceiverConfig): Promise<void> {
    const response = await fetch(`${API_URL}/api/receiver/udp/start`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(config),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || 'Failed to start UDP receiver');
    }
  }

  async startPcapReceiver(config: PcapReceiverConfig): Promise<void> {
    const response = await fetch(`${API_URL}/api/receiver/pcap/start`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(config),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || 'Failed to start PCAP receiver');
    }
  }

  async stopReceiver(): Promise<void> {
    const response = await fetch(`${API_URL}/api/receiver/stop`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || 'Failed to stop receiver');
    }
  }

  async getReceiverStatus(): Promise<ReceiverStatus> {
    const response = await fetch(`${API_URL}/api/receiver/status`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Failed to get receiver status');
    }

    return await response.json();
  }
}

export const receiverService = new ReceiverService();

