import { useState, useEffect } from 'react';
import { Box, Typography } from '@mui/material';

interface TimestampProps {
  timestamp: string;
}

export const Timestamp = ({ timestamp }: TimestampProps) => {
  const [relativeTime, setRelativeTime] = useState<string>('');

  useEffect(() => {
    const updateRelativeTime = () => {
      const date = new Date(timestamp);
      const now = new Date();
      const diffMs = now.getTime() - date.getTime();
      const diffSeconds = Math.floor(diffMs / 1000);
      const diffMinutes = Math.floor(diffSeconds / 60);
      const diffHours = Math.floor(diffMinutes / 60);
      const diffDays = Math.floor(diffHours / 24);

      if (diffSeconds < 60) {
        setRelativeTime('just now');
      } else if (diffMinutes < 60) {
        setRelativeTime(`${diffMinutes} minute${diffMinutes !== 1 ? 's' : ''} ago`);
      } else if (diffHours < 24) {
        setRelativeTime(`${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`);
      } else {
        setRelativeTime(`${diffDays} day${diffDays !== 1 ? 's' : ''} ago`);
      }
    };

    updateRelativeTime();
    const interval = setInterval(updateRelativeTime, 60000);

    return () => clearInterval(interval);
  }, [timestamp]);

  const formatAbsoluteTime = (timestamp: string): string => {
    const date = new Date(timestamp);
    return date.toLocaleString();
  };

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
      <Typography variant="body2" color="text.secondary" component="span">
        {relativeTime}
      </Typography>
      <Typography 
        variant="body2" 
        color="text.secondary" 
        component="span"
        sx={{ 
          fontFamily: 'monospace',
          fontSize: '0.75rem',
          opacity: 0.7
        }}
      >
        ({formatAbsoluteTime(timestamp)})
      </Typography>
    </Box>
  );
};
