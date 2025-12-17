import { useState, useEffect } from 'react';
import { useSignalR } from './hooks/useSignalR';
import { DataList } from './components/DataList';
import { ModeSelector, ReceiverMode } from './components/ModeSelector';
import { MessageModeSelector } from './components/MessageModeSelector';
import { UdpConfigForm } from './components/UdpConfigForm';
import { PcapConfigForm } from './components/PcapConfigForm';
import { ReceiverStatus } from './components/ReceiverStatus';
import { receiverService } from './services/receiverService';
import { messageModeService } from './services/messageModeService';
import { UdpReceiverConfig, PcapReceiverConfig, ReceiverStatus as ReceiverStatusType } from './types/receiver';
import { MessageMode } from './types/messageMode';
import {
  Box,
  AppBar,
  Toolbar,
  Typography,
  Alert,
  Container,
} from '@mui/material';

function App() {
  const { data, error, clearData } = useSignalR();
  const [selectedMode, setSelectedMode] = useState<ReceiverMode>(null);
  const [receiverStatus, setReceiverStatus] = useState<ReceiverStatusType>({
    mode: null,
    isRunning: false,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const [statusError, setStatusError] = useState<string | null>(null);
  const [messageMode, setMessageMode] = useState<MessageMode>('Default');

  // Load receiver status on mount and periodically
  useEffect(() => {
    const loadStatus = async () => {
      try {
        const status = await receiverService.getReceiverStatus();
        setReceiverStatus(status);
        if (status.mode) {
          setSelectedMode(status.mode);
        }
      } catch (err) {
        console.error('Failed to load receiver status:', err);
      }
    };

    loadStatus();
    const interval = setInterval(loadStatus, 5000); // Refresh every 5 seconds

    return () => clearInterval(interval);
  }, []);

  // Load message mode on mount
  useEffect(() => {
    const loadMessageMode = async () => {
      try {
        const mode = await messageModeService.getMessageMode();
        setMessageMode(mode);
      } catch (err) {
        console.error('Failed to load message mode:', err);
      }
    };

    loadMessageMode();
  }, []);

  const handleModeChange = (mode: ReceiverMode) => {
    setSelectedMode(mode);
  };

  const handleMessageModeChange = async (mode: MessageMode) => {
    try {
      await messageModeService.setMessageMode(mode);
      setMessageMode(mode);
    } catch (err) {
      setStatusError(err instanceof Error ? err.message : 'Failed to update message mode');
      console.error('Failed to update message mode:', err);
    }
  };

  const handleUdpSubmit = async (config: UdpReceiverConfig) => {
    setIsSubmitting(true);
    setStatusError(null);
    try {
      await receiverService.startUdpReceiver(config);
      const status = await receiverService.getReceiverStatus();
      setReceiverStatus(status);
    } catch (err) {
      setStatusError(err instanceof Error ? err.message : 'Failed to start UDP receiver');
      throw err;
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePcapSubmit = async (config: PcapReceiverConfig) => {
    setIsSubmitting(true);
    setStatusError(null);
    try {
      await receiverService.startPcapReceiver(config);
      const status = await receiverService.getReceiverStatus();
      setReceiverStatus(status);
    } catch (err) {
      setStatusError(err instanceof Error ? err.message : 'Failed to start PCAP receiver');
      throw err;
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleStopReceiver = async () => {
    setIsStopping(true);
    setStatusError(null);
    try {
      await receiverService.stopReceiver();
      const status = await receiverService.getReceiverStatus();
      setReceiverStatus(status);
      setSelectedMode(null);
    } catch (err) {
      setStatusError(err instanceof Error ? err.message : 'Failed to stop receiver');
    } finally {
      setIsStopping(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static" sx={{ backgroundColor: 'background.paper', borderBottom: 1, borderColor: 'divider' }}>
        <Toolbar sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h1" component="h1" sx={{ fontSize: '1.75rem', fontWeight: 600 }}>
            Asterix Reader
          </Typography>
          <MessageModeSelector mode={messageMode} onModeChange={handleMessageModeChange} compact />
        </Toolbar>
      </AppBar>
      {error && (
        <Alert severity="error" sx={{ borderRadius: 0 }}>
          Error: {error}
        </Alert>
      )}
      {statusError && (
        <Alert severity="error" sx={{ borderRadius: 0 }} onClose={() => setStatusError(null)}>
          {statusError}
        </Alert>
      )}
      <Box component="main" sx={{ flex: 1, display: 'flex', justifyContent: 'center', p: 3, overflowY: 'auto' }}>
        <Container maxWidth="lg" sx={{ width: '100%' }}>
          <ReceiverStatus
            status={receiverStatus}
            onStop={handleStopReceiver}
            isStopping={isStopping}
          />
          
          {!receiverStatus.isRunning && (
            <>
              <ModeSelector mode={selectedMode} onModeChange={handleModeChange} />
              
              {selectedMode === 'UDP' && (
                <UdpConfigForm onSubmit={handleUdpSubmit} isSubmitting={isSubmitting} />
              )}
              
              {selectedMode === 'PCAP' && (
                <PcapConfigForm onSubmit={handlePcapSubmit} isSubmitting={isSubmitting} />
              )}
            </>
          )}

          <DataList data={data} onClearAll={clearData} />
        </Container>
      </Box>
    </Box>
  );
}

export default App;
