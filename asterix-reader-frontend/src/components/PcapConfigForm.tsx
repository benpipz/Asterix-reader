import { useState } from 'react';
import {
  Box,
  TextField,
  Button,
  Alert,
  Paper,
  Typography,
} from '@mui/material';
import { PcapReceiverConfig } from '../types/receiver';

interface PcapConfigFormProps {
  onSubmit: (config: PcapReceiverConfig) => Promise<void>;
  isSubmitting?: boolean;
}

export const PcapConfigForm = ({ onSubmit, isSubmitting = false }: PcapConfigFormProps) => {
  const [filePath, setFilePath] = useState<string>('');
  const [filter, setFilter] = useState<string>('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Get the full path - in browser this will be just the filename
      // Since frontend/backend are on same machine, we'll need to handle this
      // For now, we'll use the file name and let user enter full path if needed
      setFilePath(file.name);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});
    setSubmitError(null);

    const newErrors: Record<string, string> = {};

    if (!filePath.trim()) {
      newErrors.filePath = 'File path is required';
    } else {
      const extension = filePath.toLowerCase().substring(filePath.lastIndexOf('.'));
      if (extension !== '.pcap' && extension !== '.pcapng') {
        newErrors.filePath = 'File must be a .pcap or .pcapng file';
      }
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      const config: PcapReceiverConfig = {
        filePath: filePath.trim(),
        filter: filter.trim() || undefined,
      };
      await onSubmit(config);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Failed to start PCAP receiver');
    }
  };

  return (
    <Paper sx={{ p: 3, mb: 3 }}>
      <Typography variant="h6" gutterBottom>
        PCAP File Configuration
      </Typography>
      <form onSubmit={handleSubmit}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Box>
            <input
              type="file"
              accept=".pcap,.pcapng"
              onChange={handleFileSelect}
              style={{ marginBottom: 8 }}
            />
            <TextField
              label="File Path"
              value={filePath}
              onChange={(e) => setFilePath(e.target.value)}
              error={!!errors.filePath}
              helperText={errors.filePath || 'Enter the full path to the PCAP file (e.g., C:\\path\\to\\file.pcap)'}
              required
              fullWidth
            />
          </Box>

          <TextField
            label="Filter"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            helperText='Wireshark-style filter (e.g., "udp port 5000")'
            placeholder="e.g., udp port 5000"
            fullWidth
          />

          {submitError && (
            <Alert severity="error">{submitError}</Alert>
          )}

          <Button
            type="submit"
            variant="contained"
            disabled={isSubmitting}
            sx={{ alignSelf: 'flex-start' }}
          >
            {isSubmitting ? 'Processing...' : 'Start PCAP Processing'}
          </Button>
        </Box>
      </form>
    </Paper>
  );
};

