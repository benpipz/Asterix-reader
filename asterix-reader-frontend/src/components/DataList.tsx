import { useState } from 'react';
import { ReceivedData } from '../types/data';
import { DataItem } from './DataItem';
import { ConfirmModal } from './ConfirmModal';
import { FileNameModal } from './FileNameModal';
import {
  Box,
  Typography,
  Button,
  Chip,
  Alert,
  Stack,
  Paper,
} from '@mui/material';
import { Save, Delete } from '@mui/icons-material';

interface DataListProps {
  data: ReceivedData[];
  onClearAll: () => Promise<void>;
}

export const DataList = ({ data, onClearAll }: DataListProps) => {
  const [isClearing, setIsClearing] = useState(false);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const [showFileNameModal, setShowFileNameModal] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleClearAllClick = () => {
    if (data.length === 0) return;
    setShowConfirmModal(true);
    setError(null);
  };

  const handleConfirm = async () => {
    setShowConfirmModal(false);
    setIsClearing(true);
    setError(null);
    
    try {
      await onClearAll();
    } catch (error) {
      console.error('Failed to clear data:', error);
      setError('Failed to clear data. Please try again.');
    } finally {
      setIsClearing(false);
    }
  };

  const handleCancel = () => {
    setShowConfirmModal(false);
    setError(null);
  };

  const handleSaveToFileClick = () => {
    if (data.length === 0) return;
    setShowFileNameModal(true);
    setError(null);
  };

  const handleSaveFile = (fileName: string) => {
    setShowFileNameModal(false);
    
    try {
      const jsonData = JSON.stringify(data, null, 2);
      const blob = new Blob([jsonData], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to save file:', error);
      setError('Failed to save file. Please try again.');
    }
  };

  const handleCancelFileName = () => {
    setShowFileNameModal(false);
  };

  const getDefaultFileName = () => {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
    return `asterix-data-${timestamp}`;
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h2">Received Data</Typography>
        <Stack direction="row" spacing={1} alignItems="center">
          <Chip label={`${data.length} item${data.length !== 1 ? 's' : ''}`} variant="outlined" />
          {data.length > 0 && (
            <>
              <Button
                variant="contained"
                startIcon={<Save />}
                onClick={handleSaveToFileClick}
                title="Save all data to JSON file"
              >
                Save to File
              </Button>
              <Button
                variant="contained"
                color="error"
                startIcon={<Delete />}
                onClick={handleClearAllClick}
                disabled={isClearing}
                title="Delete all data from backend"
              >
                {isClearing ? 'Clearing...' : 'Clear All'}
              </Button>
            </>
          )}
        </Stack>
      </Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      <Stack spacing={2}>
        {data.length === 0 ? (
          <Paper sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="body1" color="text.secondary" gutterBottom>
              No data received yet.
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Waiting for data from UDP or PCAP receiver...
            </Typography>
          </Paper>
        ) : (
          [...data].sort((a, b) => 
            new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
          ).map((item) => <DataItem key={item.id} data={item} />)
        )}
      </Stack>
      <ConfirmModal
        isOpen={showConfirmModal}
        title="Clear All Data"
        message={`Are you sure you want to delete all ${data.length} item${data.length !== 1 ? 's' : ''}? This action cannot be undone.`}
        confirmText="Delete All"
        cancelText="Cancel"
        onConfirm={handleConfirm}
        onCancel={handleCancel}
        isDestructive={true}
      />
      <FileNameModal
        isOpen={showFileNameModal}
        defaultName={getDefaultFileName()}
        onConfirm={handleSaveFile}
        onCancel={handleCancelFileName}
      />
    </Box>
  );
};
