import { Tabs, Tab, Box } from '@mui/material';

export type ReceiverMode = 'UDP' | 'PCAP' | null;

interface ModeSelectorProps {
  mode: ReceiverMode;
  onModeChange: (mode: ReceiverMode) => void;
}

export const ModeSelector = ({ mode, onModeChange }: ModeSelectorProps) => {
  const handleChange = (_event: React.SyntheticEvent, newValue: number) => {
    if (newValue === 0) {
      onModeChange('UDP');
    } else if (newValue === 1) {
      onModeChange('PCAP');
    }
  };

  const selectedIndex = mode === 'UDP' ? 0 : mode === 'PCAP' ? 1 : -1;

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
      <Tabs 
        value={selectedIndex >= 0 ? selectedIndex : false} 
        onChange={handleChange}
        variant="fullWidth"
        sx={{
          gap: 2,
          '& .MuiTabs-flexContainer': {
            gap: 2,
          },
          '& .MuiTab-root': {
            fontWeight: 700,
            fontSize: '1.1rem',
            textTransform: 'none',
            minHeight: 64,
            py: 2,
            px: 3,
            borderRadius: 2,
            transition: 'all 0.3s ease',
            border: 2,
            flex: 1,
            '&:hover': {
              transform: 'translateY(-2px)',
              boxShadow: 4,
            },
            '&:nth-of-type(1)': {
              // UDP Receiver tab
              bgcolor: selectedIndex === 0 ? 'primary.light' : 'action.selected',
              borderColor: selectedIndex === 0 ? 'primary.main' : 'divider',
              color: selectedIndex === 0 ? 'primary.main' : 'text.primary',
              '&:hover': {
                bgcolor: selectedIndex === 0 ? 'primary.light' : 'action.hover',
                borderColor: 'primary.main',
              },
            },
            '&:nth-of-type(2)': {
              // PCAP File tab
              bgcolor: selectedIndex === 1 ? 'secondary.light' : 'action.selected',
              borderColor: selectedIndex === 1 ? 'secondary.main' : 'divider',
              color: selectedIndex === 1 ? 'secondary.main' : 'text.primary',
              '&:hover': {
                bgcolor: selectedIndex === 1 ? 'secondary.light' : 'action.hover',
                borderColor: 'secondary.main',
              },
            },
          },
          '& .Mui-selected': {
            fontWeight: 800,
            boxShadow: 4,
            '&:hover': {
              transform: 'translateY(-2px)',
            },
          },
          '& .MuiTabs-indicator': {
            display: 'none',
          },
        }}
      >
        <Tab label="UDP Receiver" />
        <Tab label="PCAP File" />
      </Tabs>
    </Box>
  );
};

