# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["AsterixReader.Backend.csproj", "./"]
RUN dotnet restore "AsterixReader.Backend.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "AsterixReader.Backend.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AsterixReader.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install wget for healthcheck and libpcap for SharpPcap
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget libpcap0.8 \
    && rm -rf /var/lib/apt/lists/*

# Expose ports
EXPOSE 5000
EXPOSE 5000/udp

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTPS_PORT=

ENTRYPOINT ["dotnet", "AsterixReader.Backend.dll"]

