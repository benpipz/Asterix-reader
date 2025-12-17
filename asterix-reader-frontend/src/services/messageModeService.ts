import { MessageMode } from '../types/messageMode';

const API_URL = import.meta.env.VITE_API_URL || '';

class MessageModeService {
  async getMessageMode(): Promise<MessageMode> {
    const response = await fetch(`${API_URL}/api/messagemode`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Failed to get message mode');
    }

    const result = await response.json();
    return result.mode as MessageMode;
  }

  async setMessageMode(mode: MessageMode): Promise<void> {
    const response = await fetch(`${API_URL}/api/messagemode`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ mode }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || 'Failed to update message mode');
    }
  }
}

export const messageModeService = new MessageModeService();

