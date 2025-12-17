using AsterixReader.Backend.Configuration;
using AsterixReader.Backend.Hubs;
using AsterixReader.Backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace AsterixReader.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAsterixReaderServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure UDP Settings
        services.Configure<UdpSettings>(
            configuration.GetSection("UdpSettings"));

        // Add controllers and API explorer
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Add SignalR with camelCase JSON serialization
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        }).AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.WriteIndented = false;
        });

        // Add CORS
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" };

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Register services
        services.AddSingleton<IDataStorageService, DataStorageService>();
        services.AddScoped<DataProcessingService>();
        
        // Register receiver services (Transient - created on demand)
        services.AddTransient<UdpDataReceiverService>();
        services.AddTransient<PcapDataReceiverService>();
        
        // Register receiver manager (Singleton - manages receiver lifecycle)
        services.AddSingleton<IReceiverManagerService, ReceiverManagerService>();

        // Register message mode service (Singleton - stores current message mode state)
        services.AddSingleton<IMessageModeService, MessageModeService>();

        // Note: DataReceiverBackgroundService is no longer needed
        // Receivers are started on-demand via API endpoints

        return services;
    }
}

