import { ToggleButtonGroup, ToggleButton, Box } from '@mui/material';
import { MessageMode } from '../types/messageMode';

interface MessageModeSelectorProps {
  mode: MessageMode;
  onModeChange: (mode: MessageMode) => void;
  compact?: boolean;
}

export const MessageModeSelector = ({ mode, onModeChange, compact = false }: MessageModeSelectorProps) => {
  const handleChange = (_event: React.MouseEvent<HTMLElement>, newMode: MessageMode | null) => {
    if (newMode !== null) {
      onModeChange(newMode);
    }
  };

  if (compact) {
    // Compact version for navbar
    return (
      <ToggleButtonGroup
        value={mode}
        exclusive
        onChange={handleChange}
        size="small"
        sx={{
          ml: 3,
          '& .MuiToggleButton-root': {
            px: 1.5,
            py: 0.5,
            fontSize: '0.75rem',
            fontWeight: 500,
            textTransform: 'none',
            border: 1,
            borderColor: 'divider',
            '&:hover': {
              bgcolor: 'action.hover',
            },
            '&:nth-of-type(1)': {
              // Default button
              '&.Mui-selected': {
                bgcolor: 'primary.main',
                color: 'primary.contrastText',
                borderColor: 'primary.main',
                '&:hover': {
                  bgcolor: 'primary.dark',
                },
              },
            },
            '&:nth-of-type(2)': {
              // Incoming button
              '&.Mui-selected': {
                bgcolor: 'success.main',
                color: 'success.contrastText',
                borderColor: 'success.main',
                '&:hover': {
                  bgcolor: 'success.dark',
                },
              },
            },
            '&:nth-of-type(3)': {
              // Outgoing button
              '&.Mui-selected': {
                bgcolor: 'warning.main',
                color: 'warning.contrastText',
                borderColor: 'warning.main',
                '&:hover': {
                  bgcolor: 'warning.dark',
                },
              },
            },
          },
        }}
      >
        <ToggleButton value="Default">Default</ToggleButton>
        <ToggleButton value="Incoming">Incoming</ToggleButton>
        <ToggleButton value="Outgoing">Outgoing</ToggleButton>
      </ToggleButtonGroup>
    );
  }

  // Full version (kept for backward compatibility)
  return (
    <Box 
      sx={{ 
        mb: 4,
        p: 2,
        borderRadius: 2,
        bgcolor: 'background.paper',
        boxShadow: 2,
      }}
    >
      <ToggleButtonGroup
        value={mode}
        exclusive
        onChange={handleChange}
        fullWidth
        sx={{
          '& .MuiToggleButton-root': {
            px: 3,
            py: 1.5,
            fontSize: '1rem',
            fontWeight: 600,
            textTransform: 'none',
            border: 2,
            '&:hover': {
              bgcolor: 'action.hover',
            },
            '&:nth-of-type(1)': {
              // Default button
              '&.Mui-selected': {
                bgcolor: 'primary.main',
                color: 'primary.contrastText',
                borderColor: 'primary.main',
                '&:hover': {
                  bgcolor: 'primary.dark',
                },
              },
            },
            '&:nth-of-type(2)': {
              // Incoming button
              '&.Mui-selected': {
                bgcolor: 'success.main',
                color: 'success.contrastText',
                borderColor: 'success.main',
                '&:hover': {
                  bgcolor: 'success.dark',
                },
              },
            },
            '&:nth-of-type(3)': {
              // Outgoing button
              '&.Mui-selected': {
                bgcolor: 'warning.main',
                color: 'warning.contrastText',
                borderColor: 'warning.main',
                '&:hover': {
                  bgcolor: 'warning.dark',
                },
              },
            },
          },
        }}
      >
        <ToggleButton value="Default">Default</ToggleButton>
        <ToggleButton value="Incoming">Incoming</ToggleButton>
        <ToggleButton value="Outgoing">Outgoing</ToggleButton>
      </ToggleButtonGroup>
    </Box>
  );
};

