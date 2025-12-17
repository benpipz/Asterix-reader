# Asterix Reader

A real-time data monitoring application with a TypeScript React frontend and ASP.NET 8.0 backend. The backend receives data via UDP sockets or PCAP files, processes it into JSON objects, and pushes updates to the frontend in real-time via SignalR.

## Architecture

```
[UDP Socket / PCAP File] → [Data Receiver Service] → [Data Processing Service] → [SignalR Hub] → [React Frontend]
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

- **UDP Settings**: Port, listen address, and buffer size
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

The frontend connects to the backend SignalR hub. The connection URL is configured in `src/services/signalRService.ts` and defaults to the backend URL.

## Testing

### Testing UDP Reception

You can test the UDP receiver using various methods:

**Using PowerShell (Windows):**
```powershell
$udpClient = New-Object System.Net.Sockets.UdpClient
$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse("127.0.0.1"), 5000)
$data = [System.Text.Encoding]::UTF8.GetBytes('{"test": "Hello World", "value": 123}')
$udpClient.Send($data, $data.Length, $endpoint)
$udpClient.Close()
```

**Using Python:**
```python
import socket
import json

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
data = json.dumps({"test": "Hello World", "value": 123}).encode('utf-8')
sock.sendto(data, ('localhost', 5000))
sock.close()
```

**Using netcat (Linux/Mac):**
```bash
echo '{"test": "Hello World", "value": 123}' | nc -u localhost 5000
```

### Testing PCAP Processing

1. Upload a PCAP file using the frontend interface or the API endpoint `/api/receiver/pcap/upload`
2. Start the PCAP receiver with the file path
3. Optionally specify a BPF filter (e.g., `udp port 5000`) or display filter (e.g., `udp.port == 5000`)

### Testing with Mock Data

Use the API endpoint to generate mock data:
```bash
curl -X POST http://localhost:5000/api/data/mock?depth=4
```

## Features

- **Multiple Data Sources**: 
  - UDP socket receiver (with multicast support)
  - PCAP file processor (supports .pcap and .pcapng files with BPF or display filters)
- **Real-time Updates**: Data received is immediately pushed to all connected frontend clients via SignalR
- **Message Modes**: Filter messages by mode (Default, Incoming, Outgoing)
- **JSON Viewer**: Expandable/collapsible JSON viewer with syntax highlighting
- **Timestamp Display**: Shows relative time (e.g., "2 minutes ago") with absolute time on hover
- **In-Memory Storage**: All received data is stored in memory (cleared on restart)
- **Data Export**: Save all received data to JSON file
- **REST API**: Additional endpoints for data retrieval and management:
  - `GET /api/data` - Get all data
  - `GET /api/data/{id}` - Get specific data by ID
  - `GET /api/data/count` - Get total count
  - `DELETE /api/data` - Clear all data
  - `POST /api/data/send` - Send custom JSON data
  - `POST /api/data/mock` - Generate mock data for testing
  - `GET /api/receiver/status` - Get receiver status
  - `POST /api/receiver/udp/start` - Start UDP receiver
  - `POST /api/receiver/pcap/start` - Start PCAP receiver
  - `POST /api/receiver/pcap/upload` - Upload PCAP file
  - `POST /api/receiver/stop` - Stop receiver
  - `GET /api/messagemode` - Get current message mode
  - `PUT /api/messagemode` - Set message mode

## Project Structure

### Backend
```
AsterixReader.Backend/
├── Controllers/          # REST API controllers (Data, Receiver, MessageMode)
├── Hubs/                # SignalR hubs (DataHub)
├── Services/            # Business logic services
│   ├── DataReceiverService implementations (UDP, PCAP)
│   ├── DataProcessingService
│   ├── DataStorageService
│   ├── ReceiverManagerService
│   └── MessageModeService
├── Models/              # Data models
├── Configuration/       # Configuration classes
├── Extensions/          # Service collection extensions
└── Program.cs           # Application entry point
```

### Frontend
```
asterix-reader-frontend/
├── src/
│   ├── components/      # React components
│   │   ├── DataList, DataItem
│   │   ├── ModeSelector, MessageModeSelector
│   │   ├── UdpConfigForm, PcapConfigForm
│   │   └── ReceiverStatus, JsonViewer
│   ├── services/        # API services (SignalR, Receiver, MessageMode)
│   ├── hooks/           # Custom React hooks (useSignalR)
│   ├── types/           # TypeScript type definitions
│   └── theme.ts         # Material-UI theme configuration
```

## Extensibility

The application is designed to be extensible:

- **New Data Sources**: Implement `IDataReceiverService` interface
- **New Deserializers**: Add deserialization logic in `DataProcessingService`
- **Custom Processing**: Extend `DataProcessingService` for custom data transformation
- **Message Modes**: Extend message mode functionality for custom filtering

## Notes

- Data is stored in-memory and will be lost on restart
- UDP is connectionless - handle packet loss gracefully
- The application handles both JSON strings and binary data
- SignalR automatically reconnects on connection loss
- PCAP files are processed sequentially and automatically stop when the file ends
- BPF filters are more efficient than display filters as they're applied at the libpcap level

## Docker Support

The project includes Docker support with separate Dockerfiles for backend and frontend. See `docker-compose.yml` for orchestration.

## License

MIT

