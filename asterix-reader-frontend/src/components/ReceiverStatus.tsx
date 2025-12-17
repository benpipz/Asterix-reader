import { Box, Paper, Typography, Button, Chip, Alert } from '@mui/material';
import { ReceiverStatus as ReceiverStatusType, UdpReceiverConfig, PcapReceiverConfig } from '../types/receiver';

interface ReceiverStatusProps {
  status: ReceiverStatusType;
  onStop: () => Promise<void>;
  isStopping?: boolean;
}

export const ReceiverStatus = ({ status, onStop, isStopping = false }: ReceiverStatusProps) => {
  if (!status.isRunning && !status.mode) {
    return (
      <Paper sx={{ p: 2, mb: 3 }}>
        <Alert severity="info">No receiver is currently running. Select a mode and configure to start.</Alert>
      </Paper>
    );
  }

  const getModeColor = (mode: string | null) => {
    switch (mode) {
      case 'UDP':
        return 'primary';
      case 'PCAP':
        return 'secondary';
      default:
        return 'default';
    }
  };

  return (
    <Paper sx={{ p: 2, mb: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
        <Box>
          <Typography variant="h6" gutterBottom>
            Receiver Status
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <Chip
              label={status.mode || 'None'}
              color={getModeColor(status.mode) as any}
              variant={status.isRunning ? 'filled' : 'outlined'}
            />
            <Chip
              label={status.isRunning ? 'Running' : 'Stopped'}
              color={status.isRunning ? 'success' : 'default'}
              variant="outlined"
            />
          </Box>
          {status.config && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                {status.mode === 'UDP' && (() => {
                  const udpConfig = status.config as UdpReceiverConfig;
                  return (
                    <>
                      Port: {udpConfig.port}, Address: {udpConfig.listenAddress}
                      {udpConfig.joinMulticastGroup && udpConfig.multicastAddress && (
                        <> • Multicast: {udpConfig.multicastAddress}</>
                      )}
                    </>
                  );
                })()}
                {status.mode === 'PCAP' && (() => {
                  const pcapConfig = status.config as PcapReceiverConfig;
                  return (
                    <>
                      File: {pcapConfig.filePath}
                      {pcapConfig.filter && <> • Filter: {pcapConfig.filter}</>}
                    </>
                  );
                })()}
              </Typography>
            </Box>
          )}
        </Box>
        {status.isRunning && (
          <Button
            variant="outlined"
            color="error"
            onClick={onStop}
            disabled={isStopping}
          >
            {isStopping ? 'Stopping...' : 'Stop Receiver'}
          </Button>
        )}
      </Box>
    </Paper>
  );
};

