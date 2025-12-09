import { useSignalR } from './hooks/useSignalR';
import { DataList } from './components/DataList';
import {
  Box,
  AppBar,
  Toolbar,
  Typography,
  Alert,
  Container,
} from '@mui/material';

function App() {
  const { data, error, clearData } = useSignalR();

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static" sx={{ backgroundColor: 'background.paper', borderBottom: 1, borderColor: 'divider' }}>
        <Toolbar sx={{ justifyContent: 'center' }}>
          <Typography variant="h1" component="h1" sx={{ fontSize: '1.75rem', fontWeight: 600 }}>
            ✨ Asterix Reader
          </Typography>
        </Toolbar>
      </AppBar>
      {error && (
        <Alert severity="error" sx={{ borderRadius: 0 }}>
          Error: {error}
        </Alert>
      )}
      <Box component="main" sx={{ flex: 1, display: 'flex', justifyContent: 'center', p: 3, overflowY: 'auto' }}>
        <Container maxWidth="lg" sx={{ width: '100%' }}>
          <DataList data={data} onClearAll={clearData} />
        </Container>
      </Box>
    </Box>
  );
}

export default App;
