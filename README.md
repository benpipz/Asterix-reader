# Asterix Reader

A real-time data monitoring application with a TypeScript React frontend and ASP.NET 8.0 backend. The backend receives binary data via UDP sockets, deserializes it into JSON objects, and pushes updates to the frontend in real-time.

## Architecture

```
[UDP Socket] → [ASP.NET Backend] → [SignalR Hub] → [React Frontend]
                      ↓
              [In-Memory Storage]
```

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- A UDP client to send test data (optional)

## Getting Started

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd AsterixReader.Backend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the backend:
   ```bash
   dotnet run
   ```

The backend will start on `https://localhost:5000` (or `http://localhost:5000` depending on your configuration).

### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd asterix-reader-frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

The frontend will start on `http://localhost:5173`.

## Configuration

### Backend Configuration

Edit `AsterixReader.Backend/appsettings.json` to configure:

- **UDP Port**: Default is 5000
- **Listen Address**: Default is 0.0.0.0 (all interfaces)
- **CORS Origins**: Configure allowed frontend origins

```json
{
  "UdpSettings": {
    "Port": 5000,
    "ListenAddress": "0.0.0.0",
    "BufferSize": 65507
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

### Frontend Configuration

The frontend connects to the backend SignalR hub at `http://localhost:5000/datahub`. To change this, edit `src/services/signalRService.ts`:

```typescript
const HUB_URL = 'http://localhost:5000/datahub';
```

## Testing UDP Reception

You can test the UDP receiver using various methods:

### Using PowerShell (Windows)
```powershell
$udpClient = New-Object System.Net.Sockets.UdpClient
$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse("127.0.0.1"), 5000)
$data = [System.Text.Encoding]::UTF8.GetBytes('{"test": "Hello World", "value": 123}')
$udpClient.Send($data, $data.Length, $endpoint)
$udpClient.Close()
```

### Using Python
```python
import socket
import json

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
data = json.dumps({"test": "Hello World", "value": 123}).encode('utf-8')
sock.sendto(data, ('localhost', 5000))
sock.close()
```

### Using netcat (Linux/Mac)
```bash
echo '{"test": "Hello World", "value": 123}' | nc -u localhost 5000
```

## Features

- **Real-time Updates**: Data received via UDP is immediately pushed to all connected frontend clients via SignalR
- **JSON Viewer**: Expandable/collapsible JSON viewer with syntax highlighting
- **Timestamp Display**: Shows relative time (e.g., "2 minutes ago") with absolute time on hover
- **In-Memory Storage**: All received data is stored in memory (cleared on restart)
- **REST API**: Additional endpoints for data retrieval:
  - `GET /api/data` - Get all data
  - `GET /api/data/{id}` - Get specific data by ID
  - `GET /api/data/count` - Get total count
  - `DELETE /api/data` - Clear all data

## Project Structure

### Backend
```
AsterixReader.Backend/
├── Controllers/       # REST API controllers
├── Hubs/             # SignalR hubs
├── Services/         # Business logic services
├── Models/           # Data models
├── Configuration/    # Configuration classes
└── Program.cs        # Application entry point
```

### Frontend
```
asterix-reader-frontend/
├── src/
│   ├── components/   # React components
│   ├── services/     # SignalR service
│   ├── hooks/        # Custom React hooks
│   ├── types/        # TypeScript types
│   └── styles/       # CSS styles
```

## Extensibility

The application is designed to be extensible:

- **New Data Sources**: Implement `IDataReceiverService` interface
- **New Deserializers**: Add deserialization logic in `DataProcessingService`
- **Custom Processing**: Extend `DataProcessingService` for custom data transformation

## Notes

- Data is stored in-memory and will be lost on restart
- UDP is connectionless - handle packet loss gracefully
- The application handles both JSON strings and binary data
- SignalR automatically reconnects on connection loss

## License

MIT

