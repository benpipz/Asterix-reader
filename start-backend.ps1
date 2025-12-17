# Start Backend Server
Write-Host "Starting ASP.NET Backend..." -ForegroundColor Green
Set-Location AsterixReader.Backend
dotnet run --launch-profile http
Set-Location ..


