# How to Start the Application

## Option 1: Start Both Servers Manually

### Terminal 1 - Backend:
```powershell
cd AsterixReader.Backend
dotnet run --launch-profile http
```

The backend will start on **http://localhost:5000**

### Terminal 2 - Frontend:
```powershell
cd asterix-reader-frontend
npm run dev
```

The frontend will start on **http://localhost:5173**

## Option 2: Use PowerShell Scripts

Run these in separate terminals:

```powershell
.\start-backend.ps1
```

```powershell
.\start-frontend.ps1
```

## Access the Application

1. **Frontend (Main App)**: Open http://localhost:5173 in your browser
2. **Backend API**: http://localhost:5000
3. **Swagger UI**: http://localhost:5000/swagger

## Troubleshooting

### If SignalR connection fails:

1. **Check backend is running**: Visit http://localhost:5000/swagger - you should see the Swagger UI
2. **Check CORS**: Make sure the frontend URL (http://localhost:5173) is in the backend's CORS allowed origins
3. **Check browser console**: Look for detailed error messages
4. **Verify ports**: 
   - Backend should be on port 5000 (HTTP)
   - Frontend should be on port 5173

### Common Issues:

- **Backend not running**: Start it with `dotnet run --launch-profile http`
- **Port already in use**: Change the port in `launchSettings.json` or `vite.config.ts`
- **CORS errors**: Check `appsettings.json` includes `http://localhost:5173` in allowed origins

## Testing UDP Reception

Once both servers are running, test UDP data reception:

### PowerShell:
```powershell
$udpClient = New-Object System.Net.Sockets.UdpClient
$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse("127.0.0.1"), 5000)
$data = [System.Text.Encoding]::UTF8.GetBytes('{"test": "Hello World", "value": 123}')
$udpClient.Send($data, $data.Length, $endpoint)
$udpClient.Close()
```

### Python:
```python
import socket
import json

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
data = json.dumps({"test": "Hello World", "value": 123}).encode('utf-8')
sock.sendto(data, ('localhost', 5000))
sock.close()
```

After sending UDP data, it should appear in the frontend at http://localhost:5173!

