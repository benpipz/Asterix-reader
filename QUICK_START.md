# Quick Start Guide

## The Backend is NOT Running!

You're getting `ERR_CONNECTION_REFUSED` because the backend server needs to be started.

## Step-by-Step Instructions

### 1. Start the Backend (Required!)

Open a **new terminal/PowerShell window** and run:

```powershell
cd D:\Programing\Asterix-reader\AsterixReader.Backend
dotnet run --launch-profile http
```

**Wait for this output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

**Keep this terminal window open!** The backend must stay running.

### 2. Verify Backend is Running

Open your browser and visit: **http://localhost:5000/swagger**

You should see the Swagger API documentation page. If you see this, the backend is running correctly!

### 3. Frontend Should Auto-Connect

Once the backend is running:
- Refresh your browser at **http://localhost:5173**
- The connection status (top right) should change from "Disconnected" (red) to "Connected" (green)

### 4. Test UDP Data Reception

Once both are running, send test UDP data:

**PowerShell:**
```powershell
$udpClient = New-Object System.Net.Sockets.UdpClient
$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse("127.0.0.1"), 5000)
$data = [System.Text.Encoding]::UTF8.GetBytes('{"test": "Hello World", "value": 123, "timestamp": "' + (Get-Date).ToString() + '"}')
$udpClient.Send($data, $data.Length, $endpoint)
$udpClient.Close()
```

The data should appear in your browser at http://localhost:5173!

## Troubleshooting

### Backend won't start?
- Make sure you're in the `AsterixReader.Backend` directory
- Check for port conflicts: another app might be using port 5000
- Try: `dotnet build` first to check for errors

### Still getting connection errors?
- Make sure backend shows "Now listening on: http://localhost:5000"
- Check browser console for detailed errors
- Verify CORS: `appsettings.json` should include `http://localhost:5173`

### Port 5000 already in use?
Edit `Properties/launchSettings.json` and change the port, then update `signalRService.ts` in the frontend to match.

## Summary

**You need TWO terminals running:**
1. **Terminal 1**: Backend (`dotnet run --launch-profile http`)
2. **Terminal 2**: Frontend (`npm run dev`) - should already be running

**Then access:** http://localhost:5173


