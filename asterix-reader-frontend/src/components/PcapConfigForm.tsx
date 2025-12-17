import { useState, useRef } from 'react';
import {
  Box,
  TextField,
  Button,
  Alert,
  Paper,
  Typography,
  InputAdornment,
  IconButton,
} from '@mui/material';
import { CloudUpload, Clear } from '@mui/icons-material';
import { PcapReceiverConfig } from '../types/receiver';
import { receiverService } from '../services/receiverService';

interface PcapConfigFormProps {
  onSubmit: (config: PcapReceiverConfig) => Promise<void>;
  isSubmitting?: boolean;
}

export const PcapConfigForm = ({ onSubmit, isSubmitting = false }: PcapConfigFormProps) => {
  const [filePath, setFilePath] = useState<string>('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [filter, setFilter] = useState<string>('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file extension
    const extension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
    if (extension !== '.pcap' && extension !== '.pcapng') {
      setErrors({ filePath: 'File must be a .pcap or .pcapng file' });
      return;
    }

    setSelectedFile(file);
    setErrors({});
    setSubmitError(null);
    setIsUploading(true);

    try {
      // Upload file to server
      const uploadedFilePath = await receiverService.uploadPcapFile(file);
      setFilePath(uploadedFilePath);
      console.log(`File uploaded: ${file.name} -> ${uploadedFilePath}`);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Failed to upload file');
      setSelectedFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } finally {
      setIsUploading(false);
    }
  };

  const handleClearFile = () => {
    setSelectedFile(null);
    setFilePath('');
    setErrors({});
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});
    setSubmitError(null);

    const newErrors: Record<string, string> = {};

    if (!filePath.trim()) {
      newErrors.filePath = 'Please select a PCAP file';
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
              ref={fileInputRef}
              style={{ display: 'none' }}
              id="pcap-file-input"
            />
            <label htmlFor="pcap-file-input">
              <Button
                variant="outlined"
                component="span"
                startIcon={<CloudUpload />}
                disabled={isUploading}
                sx={{ mb: 2 }}
              >
                {isUploading ? 'Uploading...' : 'Select PCAP File'}
              </Button>
            </label>
            {selectedFile && (
              <Box sx={{ mb: 2, p: 1, bgcolor: 'action.hover', borderRadius: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Typography variant="body2">
                  Selected: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)
                </Typography>
                <IconButton size="small" onClick={handleClearFile} color="error">
                  <Clear />
                </IconButton>
              </Box>
            )}
            <TextField
              label="File Path"
              value={filePath}
              onChange={(e) => setFilePath(e.target.value)}
              error={!!errors.filePath}
              helperText={
                errors.filePath || 
                (filePath ? 'File uploaded successfully. You can also manually enter a file path.' : 'Select a PCAP file using the button above, or enter the full path manually.')
              }
              placeholder="File path will be set automatically when you select a file"
              required
              fullWidth
              disabled={!!selectedFile}
              sx={{ 
                '& .MuiInputBase-input': { 
                  fontFamily: 'monospace',
                  fontSize: '0.9rem'
                } 
              }}
              InputProps={{
                endAdornment: filePath && !selectedFile ? (
                  <InputAdornment position="end">
                    <IconButton size="small" onClick={() => setFilePath('')}>
                      <Clear />
                    </IconButton>
                  </InputAdornment>
                ) : null,
              }}
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

