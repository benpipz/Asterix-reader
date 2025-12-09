# Docker Setup Guide

This project includes Docker support for easy deployment and development.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose

## Quick Start

### Build All Images

Run the build script to build all Docker images:

```bash
build.bat
```

Or manually:

```bash
docker-compose build
```

### Start Containers

```bash
docker-compose up -d
```

### View Logs

```bash
docker-compose logs -f
```

### Stop Containers

```bash
docker-compose down
```

## Services

### Backend
- **Port**: 5000 (HTTP) and 5000 (UDP)
- **URL**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/swagger/index.html

### Frontend
- **Port**: 5173
- **URL**: http://localhost:5173
- **Served via**: Nginx

## Architecture

```
┌─────────────────┐
│   Frontend      │
│   (Nginx)       │
│   Port: 5173    │
└────────┬────────┘
         │ HTTP/WebSocket
         │
┌────────▼────────┐
│   Backend       │
│   (ASP.NET)     │
│   Port: 5000    │
└─────────────────┘
         │
         │ UDP
         │
┌────────▼────────┐
│  UDP Clients    │
└─────────────────┘
```

## Building Individual Images

### Backend Only

```bash
cd AsterixReader.Backend
docker build -t asterix-reader-backend:latest .
```

### Frontend Only

```bash
cd asterix-reader-frontend
docker build -t asterix-reader-frontend:latest .
```

## Environment Variables

### Backend
- `ASPNETCORE_ENVIRONMENT`: Set to `Production` in Docker
- `ASPNETCORE_URLS`: Set to `http://+:5000`

### Frontend (Build-time)
- `VITE_SIGNALR_URL`: Backend SignalR hub URL (default: http://localhost:5000/datahub)
- `VITE_API_URL`: Backend API URL (default: http://localhost:5000)

## Network

All containers run on the `asterix-network` bridge network, allowing them to communicate using service names.

## Troubleshooting

### Port Already in Use
If port 5000 or 5173 is already in use, modify the port mappings in `docker-compose.yml`:

```yaml
ports:
  - "8080:5000"  # Change host port
```

### Frontend Can't Connect to Backend
- Ensure both containers are running: `docker-compose ps`
- Check backend logs: `docker-compose logs backend`
- Verify CORS settings in `appsettings.json` include `http://localhost:5173`

### Rebuild After Code Changes
```bash
docker-compose build --no-cache
docker-compose up -d
```

## Production Considerations

For production deployment:
1. Use environment-specific configuration files
2. Set up proper secrets management
3. Configure HTTPS/TLS
4. Set up proper logging and monitoring
5. Use a reverse proxy (e.g., Traefik, Nginx) in front of containers

