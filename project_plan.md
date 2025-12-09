Checking the workspace structure, then drafting the plan document.


[1 tool called]


Drafting the plan document:


[1 tool called]


Plan document:

```markdown:PROJECT_PLAN.md
# Asterix Reader - Project Plan

## Overview
A real-time data monitoring application with a TypeScript React frontend and ASP.NET 8.0 backend. The backend receives binary data via UDP sockets, deserializes it into JSON objects, and pushes updates to the frontend in real-time.

## Architecture

### High-Level Architecture
```
[UDP Socket] → [ASP.NET Backend] → [SignalR Hub] → [React Frontend]
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
│   └── DataController.cs
├── Hubs/
│   └── DataHub.cs (SignalR)
├── Services/
│   ├── IDataReceiverService.cs
│   ├── UdpDataReceiverService.cs
│   ├── IDataStorageService.cs
│   ├── DataStorageService.cs
│   └── DataProcessingService.cs
├── Models/
│   ├── ReceivedData.cs
│   └── DataMessage.cs
├── Configuration/
│   └── UdpSettings.cs
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
- Listens for incoming UDP packets
- Raises `DataReceived` event when data arrives
- Handles socket errors gracefully

**Future Extensions**:
- `TcpDataReceiverService` for TCP connections
- `FileDataReceiverService` for file-based input
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

#### 2.5 API Controller
**Purpose**: RESTful endpoints for data retrieval

**Endpoints**:
- `GET /api/data` - Get all data
- `GET /api/data/{id}` - Get specific data by ID
- `GET /api/data/count` - Get total count
- `DELETE /api/data` - Clear all data (used by frontend Clear All button)

### 3. Configuration

#### appsettings.json
```json
{
  "UdpSettings": {
    "Port": 5000,
    "ListenAddress": "0.0.0.0",
    "BufferSize": 65507
  },
  "DataReceiver": {
    "Type": "Udp",
    "Udp": {
      "Port": 5000,
      "ListenAddress": "0.0.0.0"
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

### 4. Dependency Injection Setup
- Register `IDataReceiverService` → `UdpDataReceiverService` (configurable)
- Register `IDataStorageService` → `DataStorageService` (Singleton)
- Register `DataProcessingService` (Scoped)
- Register SignalR services
- Configure CORS for frontend

### 5. Startup Sequence
1. Configure services and dependency injection
2. Start `IDataReceiverService` as background service
3. Wire up `DataReceived` event to `DataProcessingService`
4. Configure SignalR endpoints
5. Start web server

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
│   │   ├── DataList.tsx
│   │   ├── DataItem.tsx
│   │   ├── JsonViewer.tsx
│   │   └── Timestamp.tsx
│   ├── services/
│   │   └── signalRService.ts
│   ├── hooks/
│   │   └── useSignalR.ts
│   ├── types/
│   │   └── data.ts
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

#### 2.5 Timestamp.tsx
- Formats and displays timestamp
- Shows relative time (e.g., "2 minutes ago")
- Shows absolute time on hover

### 3. SignalR Integration

#### signalRService.ts
- Manages SignalR connection
- Connects to backend hub
- Listens for `DataReceived` events
- Provides methods to fetch all data

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

### Receiving Data
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

### Initial Load
1. Frontend connects to SignalR hub
2. Calls `GetAllData()` method
3. Receives all stored data
4. Renders list

## Technology Stack

### Backend
- **Framework**: ASP.NET 8.0
- **Real-time**: SignalR
- **JSON**: System.Text.Json
- **UDP**: System.Net.Sockets.UdpClient
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
4. Implement `UdpDataReceiverService`
5. Implement `IDataStorageService` and `DataStorageService`
6. Create `ReceivedData` model
7. Set up dependency injection

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

### Phase 6: Polish & Testing
1. Add error handling
2. Add loading states
3. Improve UI/UX
4. Test end-to-end flow
5. Add unit tests (optional)

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
- Multiple data source support simultaneously
- Authentication and authorization
- Data retention policies
- WebSocket fallback for SignalR

## Notes
- In-memory storage means data is lost on restart (acceptable for Phase 1)
- UDP is connectionless - handle packet loss gracefully
- Consider rate limiting for high-frequency data
- Add validation for deserialized data
- Consider adding health check endpoints
```

Save this as `PROJECT_PLAN.md` in your workspace. It covers architecture, components, data flow, tech stack, and implementation phases. Should I expand any section or add details?