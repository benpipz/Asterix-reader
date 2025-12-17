import { useState } from 'react';
import {
  Box,
  Button,
  Typography,
  IconButton,
} from '@mui/material';
import { ContentCopy, Check, KeyboardArrowRight, KeyboardArrowDown } from '@mui/icons-material';

interface JsonViewerProps {
  json: string;
  level?: number;
  showCopyButton?: boolean;
  trailingComma?: boolean;
}

export const JsonViewer = ({ json, level = 0, showCopyButton = false, trailingComma = false }: JsonViewerProps) => {
  const [isExpanded, setIsExpanded] = useState(level === 0);
  const [copySuccess, setCopySuccess] = useState(false);

  const handleCopy = async () => {
    try {
      try {
        const parsed = JSON.parse(json);
        const formatted = JSON.stringify(parsed, null, 2);
        await navigator.clipboard.writeText(formatted);
      } catch {
        await navigator.clipboard.writeText(json);
      }
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  try {
    const parsed = JSON.parse(json);
    const isObject = typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed);
    const isArray = Array.isArray(parsed);

    if (isObject) {
      const entries = Object.entries(parsed);
      const hasNestedObjects = entries.some(([_, value]) => typeof value === 'object' && value !== null);

      return (
        <Box sx={{ display: level > 0 ? 'inline-flex' : 'block', alignItems: 'flex-start' }}>
          {showCopyButton && level === 0 && (
            <Box sx={{ mb: 1.5, display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                size="small"
                startIcon={copySuccess ? <Check /> : <ContentCopy />}
                onClick={handleCopy}
                variant="outlined"
                sx={{ textTransform: 'none' }}
              >
                {copySuccess ? 'Copied!' : 'Copy'}
              </Button>
            </Box>
          )}
          
          <Box sx={{ fontFamily: 'monospace', fontSize: '0.875rem', lineHeight: 1.6 }}>
            {/* Opening brace line */}
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              {(hasNestedObjects || entries.length > 0) && (
                <IconButton
                  size="small"
                  onClick={() => setIsExpanded(!isExpanded)}
                  sx={{
                    width: 20,
                    height: 20,
                    p: 0,
                    mr: 0.5,
                    color: 'text.secondary',
                    '&:hover': {
                      backgroundColor: 'action.hover',
                      color: 'primary.main',
                    },
                  }}
                  aria-label={isExpanded ? 'Collapse' : 'Expand'}
                >
                  {isExpanded ? (
                    <KeyboardArrowDown sx={{ fontSize: 16 }} />
                  ) : (
                    <KeyboardArrowRight sx={{ fontSize: 16 }} />
                  )}
                </IconButton>
              )}
              {(!hasNestedObjects && entries.length === 0) && <Box sx={{ width: 20, mr: 0.5 }} />}
              <Typography
                component="span"
                sx={{
                  fontFamily: 'monospace',
                  fontWeight: 'bold',
                  color: 'text.primary',
                }}
              >
                {'{'}
              </Typography>
              {!isExpanded && entries.length > 0 && (
                <Typography
                  component="span"
                  sx={{
                    fontFamily: 'monospace',
                    color: 'text.secondary',
                    ml: 1,
                    fontSize: '0.8rem',
                  }}
                >
                  {entries.length} {entries.length === 1 ? 'property' : 'properties'}
                </Typography>
              )}
            </Box>

            {/* Expanded content */}
            {isExpanded && (
              <Box>
                {entries.map(([key, value], index) => {
                  const isNested = typeof value === 'object' && value !== null;
                  const isLast = index === entries.length - 1;

                  return (
                    <Box key={key} sx={{ pl: 3 }}>
                      <Box sx={{ display: 'flex', alignItems: 'flex-start', py: 0.25 }}>
                        {/* Key */}
                        <Typography
                          component="span"
                          sx={{
                            fontFamily: 'monospace',
                            color: '#9cdcfe',
                            fontWeight: 500,
                            flexShrink: 0,
                          }}
                        >
                          "{key}"
                        </Typography>
                        {/* Colon */}
                        <Typography
                          component="span"
                          sx={{
                            fontFamily: 'monospace',
                            color: 'text.secondary',
                            mx: 0.5,
                            flexShrink: 0,
                          }}
                        >
                          :
                        </Typography>
                        {/* Value */}
                        <Box sx={{ display: 'inline-flex', alignItems: 'flex-start' }}>
                          {isNested ? (
                            <JsonViewer json={JSON.stringify(value)} level={level + 1} trailingComma={!isLast} />
                          ) : (
                            <>
                              <Typography
                                component="span"
                                sx={{
                                  fontFamily: 'monospace',
                                  color:
                                    typeof value === 'string'
                                      ? '#ce9178'
                                      : typeof value === 'number'
                                      ? '#b5cea8'
                                      : typeof value === 'boolean'
                                      ? '#569cd6'
                                      : value === null
                                      ? '#808080'
                                      : 'text.primary',
                                  whiteSpace: 'nowrap',
                                }}
                              >
                                {typeof value === 'string'
                                  ? `"${value}"`
                                  : value === null
                                  ? 'null'
                                  : String(value)}
                              </Typography>
                              {/* Comma */}
                              {!isLast && (
                                <Typography
                                  component="span"
                                  sx={{
                                    fontFamily: 'monospace',
                                    color: 'text.secondary',
                                    ml: 0.5,
                                    whiteSpace: 'nowrap',
                                  }}
                                >
                                  ,
                                </Typography>
                              )}
                            </>
                          )}
                        </Box>
                      </Box>
                    </Box>
                  );
                })}
                {/* Closing brace */}
                <Box sx={{ display: 'inline-flex', alignItems: 'center', mt: 0.5 }}>
                  <Box sx={{ width: 20, mr: 0.5 }} />
                  <Typography
                    component="span"
                    sx={{
                      fontFamily: 'monospace',
                      fontWeight: 'bold',
                      color: 'text.primary',
                    }}
                  >
                    {'}'}
                  </Typography>
                  {trailingComma && (
                    <Typography
                      component="span"
                      sx={{
                        fontFamily: 'monospace',
                        color: 'text.secondary',
                      }}
                    >
                      ,
                    </Typography>
                  )}
                </Box>
              </Box>
            )}

            {/* Closing brace when collapsed */}
            {!isExpanded && (
              <Box sx={{ display: 'flex', alignItems: 'center', mt: 0.5 }}>
                <Box sx={{ width: 20, mr: 0.5 }} />
                <Typography
                  component="span"
                  sx={{
                    fontFamily: 'monospace',
                    fontWeight: 'bold',
                    color: 'text.primary',
                  }}
                >
                  {'}'}
                </Typography>
                {trailingComma && (
                  <Typography
                    component="span"
                    sx={{
                      fontFamily: 'monospace',
                      color: 'text.secondary',
                    }}
                  >
                    ,
                  </Typography>
                )}
              </Box>
            )}
          </Box>
        </Box>
      );
    }

    if (isArray) {
      return (
        <Box sx={{ display: level > 0 ? 'inline-block' : 'block' }}>
          {showCopyButton && level === 0 && (
            <Box sx={{ mb: 1.5, display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                size="small"
                startIcon={copySuccess ? <Check /> : <ContentCopy />}
                onClick={handleCopy}
                variant="outlined"
                sx={{ textTransform: 'none' }}
              >
                {copySuccess ? 'Copied!' : 'Copy'}
              </Button>
            </Box>
          )}
          
          <Box sx={{ fontFamily: 'monospace', fontSize: '0.875rem', lineHeight: 1.6 }}>
            {/* Opening bracket line */}
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              {parsed.length > 0 && (
                <IconButton
                  size="small"
                  onClick={() => setIsExpanded(!isExpanded)}
                  sx={{
                    width: 20,
                    height: 20,
                    p: 0,
                    mr: 0.5,
                    color: 'text.secondary',
                    '&:hover': {
                      backgroundColor: 'action.hover',
                      color: 'primary.main',
                    },
                  }}
                  aria-label={isExpanded ? 'Collapse' : 'Expand'}
                >
                  {isExpanded ? (
                    <KeyboardArrowDown sx={{ fontSize: 16 }} />
                  ) : (
                    <KeyboardArrowRight sx={{ fontSize: 16 }} />
                  )}
                </IconButton>
              )}
              {parsed.length === 0 && <Box sx={{ width: 20, mr: 0.5 }} />}
              <Typography
                component="span"
                sx={{
                  fontFamily: 'monospace',
                  fontWeight: 'bold',
                  color: 'text.primary',
                }}
              >
                [
              </Typography>
              {!isExpanded && parsed.length > 0 && (
                <Typography
                  component="span"
                  sx={{
                    fontFamily: 'monospace',
                    color: 'text.secondary',
                    ml: 1,
                    fontSize: '0.8rem',
                  }}
                >
                  {parsed.length} {parsed.length === 1 ? 'item' : 'items'}
                </Typography>
              )}
            </Box>

            {/* Expanded content */}
            {isExpanded && (
              <Box>
                {parsed.map((item: any, index: number) => {
                  const isNested = typeof item === 'object' && item !== null;
                  const isLast = index === parsed.length - 1;

                  return (
                    <Box key={index} sx={{ pl: 3 }}>
                      <Box sx={{ display: 'inline-flex', alignItems: 'flex-start', py: 0.25, flexWrap: 'wrap' }}>
                        {isNested ? (
                          <JsonViewer json={JSON.stringify(item)} level={level + 1} trailingComma={!isLast} />
                        ) : (
                          <>
                            <Typography
                              component="span"
                              sx={{
                                fontFamily: 'monospace',
                                color:
                                  typeof item === 'string'
                                    ? '#ce9178'
                                    : typeof item === 'number'
                                    ? '#b5cea8'
                                    : typeof item === 'boolean'
                                    ? '#569cd6'
                                    : item === null
                                    ? '#808080'
                                    : 'text.primary',
                              }}
                            >
                              {typeof item === 'string'
                                ? `"${item}"`
                                : item === null
                                ? 'null'
                                : String(item)}
                            </Typography>
                            {!isLast && (
                              <Typography
                                component="span"
                                sx={{
                                  fontFamily: 'monospace',
                                  color: 'text.secondary',
                                  ml: 0.5,
                                }}
                              >
                                ,
                              </Typography>
                            )}
                          </>
                        )}
                      </Box>
                    </Box>
                  );
                })}
                {/* Closing bracket */}
                <Box sx={{ display: 'flex', alignItems: 'center', mt: 0.5 }}>
                  <Box sx={{ width: 20, mr: 0.5 }} />
                  <Typography
                    component="span"
                    sx={{
                      fontFamily: 'monospace',
                      fontWeight: 'bold',
                      color: 'text.primary',
                    }}
                  >
                    ]
                  </Typography>
                  {trailingComma && (
                    <Typography
                      component="span"
                      sx={{
                        fontFamily: 'monospace',
                        color: 'text.secondary',
                      }}
                    >
                      ,
                    </Typography>
                  )}
                </Box>
              </Box>
            )}

            {/* Closing bracket when collapsed */}
            {!isExpanded && (
              <Box sx={{ display: 'flex', alignItems: 'center', mt: 0.5 }}>
                <Box sx={{ width: 20, mr: 0.5 }} />
                <Typography
                  component="span"
                  sx={{
                    fontFamily: 'monospace',
                    fontWeight: 'bold',
                    color: 'text.primary',
                  }}
                >
                  ]
                </Typography>
                {trailingComma && (
                  <Typography
                    component="span"
                    sx={{
                      fontFamily: 'monospace',
                      color: 'text.secondary',
                    }}
                  >
                    ,
                  </Typography>
                )}
              </Box>
            )}
          </Box>
        </Box>
      );
    }

    // Primitive value
    return (
      <Typography
        component="span"
        sx={{
          fontFamily: 'monospace',
          color:
            typeof parsed === 'string'
              ? '#ce9178'
              : typeof parsed === 'number'
              ? '#b5cea8'
              : typeof parsed === 'boolean'
              ? '#569cd6'
              : parsed === null
              ? '#808080'
              : 'text.primary',
        }}
      >
        {typeof parsed === 'string' ? `"${parsed}"` : parsed === null ? 'null' : String(parsed)}
      </Typography>
    );
  } catch {
    return (
      <Typography
        component="pre"
        sx={{
          color: 'error.main',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-all',
          fontFamily: 'monospace',
        }}
      >
        {json}
      </Typography>
    );
  }
};
