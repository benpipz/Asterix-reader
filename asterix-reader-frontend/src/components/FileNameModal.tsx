import { useState, useEffect, useRef } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
  TextField,
  IconButton,
} from '@mui/material';
import { Close } from '@mui/icons-material';

interface FileNameModalProps {
  isOpen: boolean;
  defaultName: string;
  onConfirm: (fileName: string) => void;
  onCancel: () => void;
}

export const FileNameModal = ({
  isOpen,
  defaultName,
  onConfirm,
  onCancel,
}: FileNameModalProps) => {
  const [fileName, setFileName] = useState(defaultName);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen) {
      setFileName(defaultName);
      setTimeout(() => {
        inputRef.current?.focus();
        inputRef.current?.select();
      }, 100);
    }
  }, [isOpen, defaultName]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (fileName.trim()) {
      const finalName = fileName.trim().endsWith('.json')
        ? fileName.trim()
        : `${fileName.trim()}.json`;
      onConfirm(finalName);
    }
  };

  return (
    <Dialog open={isOpen} onClose={onCancel} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>
          Save File As
          <IconButton
            aria-label="close"
            onClick={onCancel}
            sx={{
              position: 'absolute',
              right: 8,
              top: 8,
              color: 'text.secondary',
            }}
          >
            <Close />
          </IconButton>
        </DialogTitle>
        <DialogContent>
          <TextField
            inputRef={inputRef}
            autoFocus
            fullWidth
            label="File Name"
            value={fileName}
            onChange={(e) => setFileName(e.target.value)}
            placeholder="Enter file name"
            margin="normal"
          />
          <DialogContentText variant="caption" sx={{ mt: 1 }}>
            The file will be saved as a JSON file (.json extension will be added automatically)
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={onCancel}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={!fileName.trim()}>
            Save
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
