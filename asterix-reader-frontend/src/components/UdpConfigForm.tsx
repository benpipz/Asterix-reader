import { useState } from 'react';
import {
  Box,
  TextField,
  Button,
  Checkbox,
  FormControlLabel,
  Alert,
  Paper,
  Typography,
} from '@mui/material';
import { UdpReceiverConfig } from '../types/receiver';

interface UdpConfigFormProps {
  onSubmit: (config: UdpReceiverConfig) => Promise<void>;
  isSubmitting?: boolean;
}

export const UdpConfigForm = ({ onSubmit, isSubmitting = false }: UdpConfigFormProps) => {
  const [port, setPort] = useState<number>(5000);
  const [listenAddress, setListenAddress] = useState<string>('0.0.0.0');
  const [joinMulticastGroup, setJoinMulticastGroup] = useState<boolean>(false);
  const [multicastAddress, setMulticastAddress] = useState<string>('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

  const validatePort = (value: number): string | null => {
    if (value < 1 || value > 65535) {
      return 'Port must be between 1 and 65535';
    }
    return null;
  };

  const validateIpAddress = (ip: string): string | null => {
    const ipRegex = /^(\d{1,3}\.){3}\d{1,3}$/;
    if (!ipRegex.test(ip)) {
      return 'Invalid IP address format';
    }
    const parts = ip.split('.').map(Number);
    if (parts.some(p => p < 0 || p > 255)) {
      return 'Invalid IP address range';
    }
    return null;
  };

  const validateMulticastAddress = (ip: string): string | null => {
    const ipError = validateIpAddress(ip);
    if (ipError) return ipError;
    
    const parts = ip.split('.').map(Number);
    const firstOctet = parts[0];
    if (firstOctet < 224 || firstOctet > 239) {
      return 'Multicast address must be in range 224.0.0.0 to 239.255.255.255';
    }
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});
    setSubmitError(null);

    const newErrors: Record<string, string> = {};

    const portError = validatePort(port);
    if (portError) newErrors.port = portError;

    const addressError = validateIpAddress(listenAddress);
    if (addressError) newErrors.listenAddress = addressError;

    if (joinMulticastGroup) {
      if (!multicastAddress) {
        newErrors.multicastAddress = 'Multicast address is required when joining multicast group';
      } else {
        const multicastError = validateMulticastAddress(multicastAddress);
        if (multicastError) newErrors.multicastAddress = multicastError;
      }
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      const config: UdpReceiverConfig = {
        port,
        listenAddress,
        joinMulticastGroup,
        multicastAddress: joinMulticastGroup ? multicastAddress : undefined,
      };
      await onSubmit(config);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Failed to start UDP receiver');
    }
  };

  return (
    <Paper sx={{ p: 3, mb: 3 }}>
      <Typography variant="h6" gutterBottom>
        UDP Receiver Configuration
      </Typography>
      <form onSubmit={handleSubmit}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField
            label="Port"
            type="number"
            value={port}
            onChange={(e) => setPort(parseInt(e.target.value) || 0)}
            error={!!errors.port}
            helperText={errors.port}
            required
            inputProps={{ min: 1, max: 65535 }}
          />

          <TextField
            label="Listen Address"
            value={listenAddress}
            onChange={(e) => setListenAddress(e.target.value)}
            error={!!errors.listenAddress}
            helperText={errors.listenAddress || 'IP address to bind to (0.0.0.0 for all interfaces)'}
            required
          />

          <FormControlLabel
            control={
              <Checkbox
                checked={joinMulticastGroup}
                onChange={(e) => setJoinMulticastGroup(e.target.checked)}
              />
            }
            label="Join Multicast Group"
          />

          {joinMulticastGroup && (
            <TextField
              label="Multicast Address"
              value={multicastAddress}
              onChange={(e) => setMulticastAddress(e.target.value)}
              error={!!errors.multicastAddress}
              helperText={errors.multicastAddress || 'Multicast IP address (224.0.0.0 - 239.255.255.255)'}
              required
            />
          )}

          {submitError && (
            <Alert severity="error">{submitError}</Alert>
          )}

          <Button
            type="submit"
            variant="contained"
            disabled={isSubmitting}
            sx={{ alignSelf: 'flex-start' }}
          >
            {isSubmitting ? 'Starting...' : 'Start UDP Receiver'}
          </Button>
        </Box>
      </form>
    </Paper>
  );
};

