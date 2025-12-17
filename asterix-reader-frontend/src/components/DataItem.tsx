import { useState } from 'react';
import { ReceivedData } from '../types/data';
import { Timestamp } from './Timestamp';
import { JsonViewer } from './JsonViewer';
import {
  Card,
  CardContent,
  Box,
  Typography,
  IconButton,
  Collapse,
} from '@mui/material';
import { ExpandMore, ExpandLess } from '@mui/icons-material';

interface DataItemProps {
  data: ReceivedData;
}

export const DataItem = ({ data }: DataItemProps) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const toggleExpanded = () => {
    setIsExpanded(!isExpanded);
  };

  const getMetadata = (jsonData: string, data?: any): string | null => {
    try {
      // First try to get metadata from the data object if provided
      if (data && typeof data === 'object') {
        const metadata = data.metadata || data.Metadata || data.METADATA;
        if (metadata !== undefined && metadata !== null) {
          return typeof metadata === 'object' ? JSON.stringify(metadata) : String(metadata);
        }
      }
      
      // Then try to parse jsonData
      const parsed = JSON.parse(jsonData);
      // Check for metadata field (case-insensitive, check common variations)
      const metadata = parsed.metadata || parsed.Metadata || parsed.METADATA;
      if (metadata !== undefined && metadata !== null) {
        // If metadata is an object, stringify it; otherwise use it directly
        return typeof metadata === 'object' ? JSON.stringify(metadata) : String(metadata);
      }
      return null;
    } catch {
      return null;
    }
  };

  const getPreview = (jsonData: string): string => {
    try {
      const parsed = JSON.parse(jsonData);
      const str = JSON.stringify(parsed);
      return str.length > 100 ? str.substring(0, 100) + '...' : str;
    } catch {
      return jsonData.length > 100 ? jsonData.substring(0, 100) + '...' : jsonData;
    }
  };

  return (
    <Card>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          p: 2,
          cursor: 'pointer',
          '&:hover': { backgroundColor: 'action.hover' },
        }}
        onClick={toggleExpanded}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Timestamp timestamp={data.timestamp} />
        </Box>
        <IconButton size="small" aria-label={isExpanded ? 'Collapse' : 'Expand'}>
          {isExpanded ? <ExpandLess /> : <ExpandMore />}
        </IconButton>
      </Box>
      <Collapse in={isExpanded}>
        <CardContent
          sx={{
            maxHeight: '70vh',
            overflow: 'auto',
            '&::-webkit-scrollbar': {
              width: '8px',
              height: '8px',
            },
            '&::-webkit-scrollbar-track': {
              backgroundColor: 'rgba(0, 0, 0, 0.05)',
            },
            '&::-webkit-scrollbar-thumb': {
              backgroundColor: 'rgba(0, 0, 0, 0.2)',
              borderRadius: '4px',
              '&:hover': {
                backgroundColor: 'rgba(0, 0, 0, 0.3)',
              },
            },
          }}
        >
          <JsonViewer json={data.jsonData} showCopyButton={true} />
        </CardContent>
      </Collapse>
      {!isExpanded && (
        <Box
          sx={{
            px: 2,
            pb: 1.5,
            borderTop: 1,
            borderColor: 'divider',
            cursor: 'pointer',
          }}
          onClick={toggleExpanded}
        >
          <Typography
            variant="body2"
            sx={{
              fontFamily: 'monospace',
              color: 'text.secondary',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {getMetadata(data.jsonData, data.data) || getPreview(data.jsonData)}
          </Typography>
        </Box>
      )}
    </Card>
  );
};
