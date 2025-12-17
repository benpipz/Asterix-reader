Checking the workspace structure, then drafting the plan document.


[1 tool called]


Drafting the plan document:


[1 tool called]


Plan document:

```markdown:PROJECT_PLAN.md
# Asterix Reader - Project Plan

## Overview
A real-time data monitoring application with a TypeScript React frontend and ASP.NET 8.0 backend. The application supports two data source modes: UDP sockets or PCAP file processing. Users can select their preferred mode and configure it dynamically. The backend receives binary data from the selected source, deserializes it into JSON objects, and pushes updates to the frontend in real-time.

## Architecture

### High-Level Architecture
```
[Mode Selection UI] → [Configuration Form] → [Backend API] → [Receiver Manager]
                                                                    ↓
[UDP Socket / PCAP File] → [IDataReceiverService] → [DataProcessingService] → [SignalR Hub] → [React Frontend]
                                                                    ↓
                                                            [In-Memory Storage]
```

## Backend (ASP.NET 8.0)

### 1. Project Structure
```
AsterixReader.Backend/
├── Program.cs
├── AsterixReader.Backend.csproj
├── Controllers/
│   ├── DataController.cs
│   └── ReceiverController.cs
├── Hubs/
│   └── DataHub.cs (SignalR)
├── Services/
│   ├── IDataReceiverService.cs
│   ├── UdpDataReceiverService.cs
│   ├── PcapDataReceiverService.cs
│   ├── IReceiverManagerService.cs
│   ├── ReceiverManagerService.cs
│   ├── IDataStorageService.cs
│   ├── DataStorageService.cs
│   ├── DataProcessingService.cs
│   └── DataReceiverBackgroundService.cs
├── Models/
│   ├── ReceivedData.cs
│   ├── DataMessage.cs
│   └── ReceiverStatus.cs
├── Configuration/
│   ├── UdpSettings.cs
│   ├── UdpReceiverConfig.cs
│   └── PcapReceiverConfig.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

### 2. Core Components

#### 2.1 Data Receiver Service (Extensible Design)
**Purpose**: Abstract interface for receiving byte arrays from various sources

**Interface**: `IDataReceiverService`
```csharp
public interface IDataReceiverService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
    event EventHandler<byte[]> DataReceived;
}
```

**UDP Implementation**: `UdpDataReceiverService`
- Configurable UDP port and IP address
- Supports multicast group joining
- Listens for incoming UDP packets
- Raises `DataReceived` event when data arrives
- Handles socket errors gracefully
- Supports dynamic reconfiguration

**PCAP Implementation**: `PcapDataReceiverService`
- Implements `IDataReceiverService`
- Uses SharpPcap library for PCAP file parsing
- Accepts PCAP file path and Wireshark-style filter string
- Processes packets matching the filter
- Extracts packet payloads and raises `DataReceived` events
- Handles file reading asynchronously

**Future Extensions**:
- `TcpDataReceiverService` for TCP connections
- `KafkaDataReceiverService` for Kafka streams
- `RabbitMqDataReceiverService` for RabbitMQ

#### 2.2 Data Storage Service
**Purpose**: In-memory storage of received and deserialized data

**Interface**: `IDataStorageService`
```csharp
public interface IDataStorageService
{
    void AddData(ReceivedData data);
    List<ReceivedData> GetAllData();
    ReceivedData? GetDataById(Guid id);
    void ClearData();
    int GetCount();
}
```

**Implementation**: `DataStorageService`
- Thread-safe in-memory list using `ConcurrentBag<ReceivedData>`
- Stores data with unique IDs and timestamps
- Provides methods for retrieval and management

#### 2.3 Data Processing Service
**Purpose**: Deserialize byte arrays into objects

**Responsibilities**:
- Receive byte array from `IDataReceiverService`
- Deserialize using appropriate deserializer (JSON, Protobuf, BinaryFormatter, etc.)
- Create `ReceivedData` object with:
  - Unique ID (Guid)
  - Timestamp (DateTime)
  - Deserialized JSON object
- Store in `IDataStorageService`
- Notify SignalR hub of new data

#### 2.4 SignalR Hub
**Purpose**: Real-time communication with frontend

**Hub**: `DataHub`
- Method: `GetAllData()` - Returns all stored data
- Method: `GetLatestData()` - Returns most recent data
- Event: `DataReceived` - Broadcasts new data to all connected clients
- Event: `DataUpdated` - Notifies clients of data updates

#### 2.5 Receiver Manager Service
**Purpose**: Manages active receiver instance and runtime mode switching

**Interface**: `IReceiverManagerService`
```csharp
public interface IReceiverManagerService
{
    Task StartUdpReceiverAsync(UdpReceiverConfig config, CancellationToken cancellationToken);
    Task StartPcapReceiverAsync(PcapReceiverConfig config, CancellationToken cancellationToken);
    Task StopReceiverAsync();
    ReceiverStatus GetStatus();
    string? GetCurrentMode();
}
```

**Implementation**: `ReceiverManagerService`
- Manages active receiver instance
- Handles runtime switching between UDP and PCAP modes
- Stops current receiver before starting new one
- Provides status information (current mode, running state)
- Thread-safe receiver management

#### 2.6 API Controllers

**DataController** - RESTful endpoints for data retrieval
- `GET /api/data` - Get all data
- `GET /api/data/{id}` - Get specific data by ID
- `GET /api/data/count` - Get total count
- `DELETE /api/data` - Clear all data (used by frontend Clear All button)

**ReceiverController** - Receiver management endpoints
- `POST /api/receiver/udp/start` - Start UDP receiver with configuration
  - Body: `UdpReceiverConfig` (port, listenAddress, joinMulticastGroup, multicastAddress)
- `POST /api/receiver/pcap/start` - Start PCAP receiver with configuration
  - Body: `PcapReceiverConfig` (filePath, filter)
- `POST /api/receiver/stop` - Stop current receiver
- `GET /api/receiver/status` - Get current receiver status

### 3. Configuration Models

#### UdpReceiverConfig
**File**: `AsterixReader.Backend/Configuration/UdpReceiverConfig.cs`
```csharp
public class UdpReceiverConfig
{
    public int Port { get; set; }
    public string ListenAddress { get; set; }
    public bool JoinMulticastGroup { get; set; }
    public string? MulticastAddress { get; set; }
}
```

#### PcapReceiverConfig
**File**: `AsterixReader.Backend/Configuration/PcapReceiverConfig.cs`
```csharp
public class PcapReceiverConfig
{
    public string FilePath { get; set; } = string.Empty;
    public string? Filter { get; set; }
}
```

#### ReceiverStatus
**File**: `AsterixReader.Backend/Models/ReceiverStatus.cs`
```csharp
public class ReceiverStatus
{
    public string? Mode { get; set; } // "UDP", "PCAP", or null
    public bool IsRunning { get; set; }
    public object? Config { get; set; } // Current configuration
}
```

### 4. Configuration

#### appsettings.json
```json
{
  "UdpSettings": {
    "Port": 5000,
    "ListenAddress": "0.0.0.0",
    "BufferSize": 65507
  },
  "Receiver": {
    "DefaultMode": null
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

### 5. Dependency Injection Setup
- Register `IReceiverManagerService` → `ReceiverManagerService` (Singleton)
- Register `IDataStorageService` → `DataStorageService` (Singleton)
- Register `DataProcessingService` (Scoped)
- Register `PcapDataReceiverService` (Transient - created on demand)
- Register `UdpDataReceiverService` (Transient - created on demand)
- Register SignalR services
- Configure CORS for frontend

### 6. Startup Sequence
1. Configure services and dependency injection
2. Configure SignalR endpoints
3. Start web server
4. Receiver starts on-demand via API (no automatic startup)

## Frontend (TypeScript React)

### 1. Project Structure
```
asterix-reader-frontend/
├── package.json
├── tsconfig.json
├── vite.config.ts (or webpack.config.js)
├── index.html
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── components/
│   │   ├── ModeSelector.tsx
│   │   ├── UdpConfigForm.tsx
│   │   ├── PcapConfigForm.tsx
│   │   ├── ReceiverStatus.tsx
│   │   ├── DataList.tsx
│   │   ├── DataItem.tsx
│   │   ├── JsonViewer.tsx
│   │   ├── Timestamp.tsx
│   │   ├── ConfirmModal.tsx
│   │   └── FileNameModal.tsx
│   ├── services/
│   │   ├── signalRService.ts
│   │   └── receiverService.ts
│   ├── hooks/
│   │   └── useSignalR.ts
│   ├── types/
│   │   ├── data.ts
│   │   └── receiver.ts
│   └── styles/
│       └── App.css
└── public/
```

### 2. Core Components

#### 2.1 App.tsx
- Main application component
- Sets up SignalR connection
- Manages global state for data list
- Handles real-time updates
- **Mode Selection UI** - Allows users to choose between UDP and PCAP modes
- **Configuration Forms** - Shows appropriate form based on selected mode
- **Receiver Status** - Displays current receiver status and allows stopping

#### 2.2 DataList.tsx
- Displays list of all received JSON objects
- Centered layout
- Scrollable container
- Shows count of items
- **Save to File button: Allows users to export all current JSON data to a file on their machine**
- **Clear All button: Allows users to delete all data from the backend storage (with confirmation modal)**

#### 2.3 DataItem.tsx
- Individual JSON item component
- Expandable/collapsible card
- Shows timestamp
- Shows JSON preview when collapsed
- Shows full JSON when expanded
- Nice visual indicators (expand/collapse icons)

#### 2.4 JsonViewer.tsx
- Pretty-prints JSON with syntax highlighting
- Handles nested objects
- Collapsible nested sections
- **Default behavior: All nested objects are collapsed by default (only top-level is expanded)**
- Copy to clipboard functionality

#### 2.6 ConfirmModal.tsx
- Custom modal component for confirmations
- Replaces browser's native alert/confirm dialogs
- Supports destructive actions with visual indicators
- Keyboard support (ESC to close)
- Smooth animations and transitions

#### 2.7 FileNameModal.tsx
- Modal component for file name input
- Allows users to customize the filename before saving
- Auto-focuses input field and selects default name
- Automatically adds .json extension if not provided
- Keyboard support (ESC to close, Enter to confirm)
- Form validation (prevents empty filenames)

#### 2.5 ModeSelector.tsx
- Radio buttons or tabs for mode selection (UDP / PCAP)
- Material UI Tabs or RadioGroup component
- Triggers display of appropriate configuration form
- Handles mode switching logic

#### 2.6 UdpConfigForm.tsx
- Form fields:
  - Port (number input, default: 5000, validation: 1-65535)
  - Listen Address (text input, default: "0.0.0.0", validation: valid IPv4)
  - Join Multicast Group (checkbox)
  - Multicast Address (text input, shown when checkbox checked, required if checked, validation: valid multicast IP 224.0.0.0 - 239.255.255.255)
- Submit button: "Start UDP Receiver"
- Form validation with error messages

#### 2.7 PcapConfigForm.tsx
- Form fields:
  - File Path (text input where user enters full file path, e.g., "C:\path\to\file.pcap" or "/path/to/file.pcap")
  - Optional: File browser button (if running in Electron/desktop environment)
  - Filter String (text input, optional, placeholder: "e.g., udp port 5000")
- Submit button: "Start PCAP Processing"
- Help text: Link to Wireshark filter documentation
- Form validation: File path required

#### 2.8 ReceiverStatus.tsx
- Displays current receiver status
- Shows current mode (UDP/PCAP/None)
- Shows running state
- "Stop Receiver" button when running
- Material UI Card or Alert component

#### 2.9 Timestamp.tsx
- Formats and displays timestamp
- Shows relative time (e.g., "2 minutes ago")
- Shows absolute time on hover

### 3. SignalR Integration

#### signalRService.ts
- Manages SignalR connection
- Connects to backend hub
- Listens for `DataReceived` events
- Provides methods to fetch all data

#### receiverService.ts
- Manages receiver operations via API
- `startUdpReceiver(config: UdpReceiverConfig): Promise<void>`
- `startPcapReceiver(filePath: string, filter?: string): Promise<void>`
- `stopReceiver(): Promise<void>`
- `getReceiverStatus(): Promise<ReceiverStatus>`
- Handles API errors and provides user feedback

#### useSignalR.ts (Custom Hook)
- React hook for SignalR connection
- Manages connection state
- Handles reconnection logic
- Returns data and connection status

### 4. Styling
- **Material UI (MUI)** - Complete UI component library
- Dark theme with custom color palette
- Modern, clean UI design
- Smooth animations for expand/collapse (built-in MUI transitions)
- Responsive layout (MUI Grid/Container system)
- Card-based design for data items (MUI Card component)
- Consistent typography and spacing (MUI theme system)
- No custom CSS - all styling through Material UI

### 5. State Management
- Use React hooks (useState, useEffect)
- Consider Context API for global state
- Or use Zustand/Redux if complexity grows

## Data Flow

### Mode Selection and Configuration
1. User selects mode (UDP or PCAP) via ModeSelector component
2. Appropriate configuration form is displayed (UdpConfigForm or PcapConfigForm)
3. User fills in configuration:
   - UDP: port, listen address, multicast settings
   - PCAP: file path, optional filter string
4. User submits configuration form
5. Frontend calls API endpoint (`/api/receiver/udp/start` or `/api/receiver/pcap/start`)
6. Backend `ReceiverManagerService`:
   - Stops current receiver if running
   - Creates new receiver instance (UdpDataReceiverService or PcapDataReceiverService)
   - Applies configuration
   - Starts receiver
   - Wires up `DataReceived` event to `DataProcessingService`

### Receiving Data (UDP Mode)
1. UDP socket receives byte array
2. `UdpDataReceiverService` raises `DataReceived` event
3. `DataProcessingService` handles event:
   - Deserializes byte array to object
   - Converts to JSON
   - Creates `ReceivedData` with ID and timestamp
   - Stores in `DataStorageService`
   - Notifies SignalR hub
4. SignalR hub broadcasts to all connected clients
5. Frontend receives update and adds to list

### Receiving Data (PCAP Mode)
1. `PcapDataReceiverService` opens PCAP file from provided path
2. Applies Wireshark-style filter if provided
3. Iterates through matching packets
4. Extracts packet payload (UDP data or raw packet data)
5. For each matching packet, raises `DataReceived` event with packet payload
6. `DataProcessingService` handles each event (same as UDP mode)
7. SignalR hub broadcasts to all connected clients
8. Frontend receives updates and adds to list

### Initial Load
1. Frontend connects to SignalR hub
2. Calls `GetReceiverStatus()` to check current receiver state
3. If receiver is running, calls `GetAllData()` method
4. Receives all stored data
5. Renders list

### Runtime Mode Switching
1. User selects new mode
2. If receiver is running, frontend calls `/api/receiver/stop`
3. Backend stops current receiver
4. Frontend waits for stop confirmation
5. Shows configuration form for new mode
6. User submits new configuration
7. New receiver starts with new configuration
8. UI updates to show new status

## Technology Stack

### Backend
- **Framework**: ASP.NET 8.0
- **Real-time**: SignalR
- **JSON**: System.Text.Json
- **UDP**: System.Net.Sockets.UdpClient
- **PCAP Processing**: SharpPcap (NuGet package)
- **Packet Parsing**: PacketDotNet (NuGet package)
- **Dependency Injection**: Built-in DI container

### Frontend
- **Framework**: React 18+
- **Language**: TypeScript
- **Build Tool**: Vite
- **SignalR Client**: @microsoft/signalr
- **UI Library**: Material UI (MUI) - Complete styling solution
- **JSON Viewer**: Custom component built with Material UI

## Implementation Phases

### Phase 1: Backend Foundation
1. Create ASP.NET 8.0 project
2. Set up project structure
3. Implement `IDataReceiverService` interface
4. Implement `UdpDataReceiverService` with multicast support
5. Implement `IDataStorageService` and `DataStorageService`
6. Create `ReceivedData` model
7. Set up dependency injection
8. Install SharpPcap and PacketDotNet NuGet packages

### Phase 2: Data Processing
1. Implement `DataProcessingService`
2. Add deserialization logic
3. Wire up event handlers
4. Test UDP reception and storage

### Phase 3: SignalR Integration
1. Create SignalR hub
2. Implement hub methods
3. Set up CORS
4. Test SignalR connection

### Phase 4: Frontend Setup
1. Create React TypeScript project
2. Install dependencies (@microsoft/signalr)
3. Set up project structure
4. Create basic layout

### Phase 5: Frontend Components
1. Implement SignalR service
2. Create DataList component
3. Create DataItem component
4. Implement expand/collapse functionality
5. Add JSON viewer
6. Style components

### Phase 6: Mode Selection & Configuration
1. Implement `ReceiverManagerService` and `IReceiverManagerService`
2. Create `PcapDataReceiverService` implementing `IDataReceiverService`
3. Create configuration models (`UdpReceiverConfig`, `PcapReceiverConfig`, `ReceiverStatus`)
4. Create `ReceiverController` with API endpoints
5. Update `UdpDataReceiverService` to support dynamic configuration and multicast
6. Implement frontend `ModeSelector` component
7. Implement `UdpConfigForm` component with validation
8. Implement `PcapConfigForm` component
9. Implement `ReceiverStatus` component
10. Create `receiverService.ts` for API calls
11. Update `App.tsx` to integrate mode selection
12. Add type definitions in `types/receiver.ts`

### Phase 7: Polish & Testing
1. Add error handling for mode switching
2. Add loading states for receiver operations
3. Validate file paths and filter strings
4. Improve UI/UX for configuration forms
5. Test end-to-end flow for both modes
6. Test runtime mode switching
7. Test multicast UDP functionality
8. Test PCAP file processing with various filters
9. Add unit tests (optional)

## Configuration & Deployment

### Development
- Backend: `http://localhost:5000`
- Frontend: `http://localhost:3000` (or 5173 for Vite)
- UDP Port: 5000 (configurable)

### Production Considerations
- Environment-specific configuration
- Logging (Serilog)
- Error handling and recovery
- Performance monitoring
- Security (authentication if needed)

## Extensibility Points

### Adding New Data Sources
1. Implement `IDataReceiverService` interface
2. Register new implementation in DI container
3. Update configuration to select implementation
4. No changes needed to other components

### Adding New Deserializers
1. Create deserializer interface
2. Implement for specific format (Protobuf, MessagePack, etc.)
3. Register in `DataProcessingService`
4. Select based on data format or configuration

## Future Enhancements
- Persistence layer (database)
- Data filtering and search
- Export functionality (CSV, JSON)
- Statistics dashboard
- Multiple data source support simultaneously (run UDP and PCAP at same time)
- Authentication and authorization
- Data retention policies
- WebSocket fallback for SignalR
- PCAP file browser/selector UI component
- Save/load receiver configurations
- Real-time PCAP file monitoring (watch for new packets in live capture files)

## Notes
- In-memory storage means data is lost on restart (acceptable for Phase 1)
- UDP is connectionless - handle packet loss gracefully
- Consider rate limiting for high-frequency data
- Add validation for deserialized data
- Consider adding health check endpoints
- PCAP files are read directly from filesystem (frontend and backend on same machine)
- File paths must be accessible to the backend process
- PCAP filter strings use Wireshark/Berkeley Packet Filter (BPF) syntax
- Multicast UDP requires proper network interface configuration
- Receiver can be stopped and restarted at runtime without restarting the application
```

Save this as `PROJECT_PLAN.md` in your workspace. It covers architecture, components, data flow, tech stack, and implementation phases. Should I expand any section or add details?