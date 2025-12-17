using AsterixReader.Backend.Configuration;
using AsterixReader.Backend.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AsterixReader.Backend.Services;

public class ReceiverManagerService : IReceiverManagerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private IDataReceiverService? _currentReceiver;
    private string? _currentMode;
    private object? _currentConfig;
    private readonly object _lock = new object();

    public ReceiverManagerService(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
    }

    public async Task StartUdpReceiverAsync(UdpReceiverConfig config, CancellationToken cancellationToken)
    {
        // Stop and dispose current receiver completely before creating new one
        IDataReceiverService? oldReceiver = null;
        lock (_lock)
        {
            oldReceiver = _currentReceiver;
            _currentReceiver = null;
            _currentMode = null;
            _currentConfig = null;
        }

        // Fully stop and dispose old receiver outside the lock
        if (oldReceiver != null)
        {
            try
            {
                await oldReceiver.StopAsync();
                if (oldReceiver is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                Console.WriteLine("Previous receiver stopped and disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing old receiver: {ex.Message}");
            }
        }

        // Create completely new UDP receiver instance
        var udpReceiver = _serviceProvider.GetRequiredService<UdpDataReceiverService>();
        udpReceiver.Configure(config);

        // Wire up event handler
        udpReceiver.DataReceived += async (sender, data) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dataProcessingService = scope.ServiceProvider.GetRequiredService<DataProcessingService>();
            await dataProcessingService.ProcessDataAsync(data);
        };

        lock (_lock)
        {
            _currentReceiver = udpReceiver;
            _currentMode = "UDP";
            _currentConfig = config;
        }

        await udpReceiver.StartAsync(cancellationToken);
        Console.WriteLine($"UDP Receiver started on {config.ListenAddress}:{config.Port}");
    }

    public async Task StartPcapReceiverAsync(PcapReceiverConfig config, CancellationToken cancellationToken)
    {
        // Stop and dispose current receiver completely before creating new one
        IDataReceiverService? oldReceiver = null;
        lock (_lock)
        {
            oldReceiver = _currentReceiver;
            _currentReceiver = null;
            _currentMode = null;
            _currentConfig = null;
        }

        // Fully stop and dispose old receiver outside the lock
        if (oldReceiver != null)
        {
            try
            {
                await oldReceiver.StopAsync();
                if (oldReceiver is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                Console.WriteLine("Previous receiver stopped and disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing old receiver: {ex.Message}");
            }
        }

        // Create completely new PCAP receiver instance
        var pcapReceiver = _serviceProvider.GetRequiredService<PcapDataReceiverService>();
        pcapReceiver.Configure(config);

        // Wire up event handler
        pcapReceiver.DataReceived += async (sender, data) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dataProcessingService = scope.ServiceProvider.GetRequiredService<DataProcessingService>();
            await dataProcessingService.ProcessDataAsync(data);
        };

        lock (_lock)
        {
            _currentReceiver = pcapReceiver;
            _currentMode = "PCAP";
            _currentConfig = config;
        }

        await pcapReceiver.StartAsync(cancellationToken);
        Console.WriteLine($"PCAP Receiver started with file: {config.FilePath}");
        if (!string.IsNullOrEmpty(config.Filter))
        {
            Console.WriteLine($"PCAP Filter: {config.Filter}");
        }
    }

    public async Task StopReceiverAsync()
    {
        IDataReceiverService? receiverToStop = null;
        
        lock (_lock)
        {
            receiverToStop = _currentReceiver;
            _currentReceiver = null;
            _currentMode = null;
            _currentConfig = null;
        }
        
        if (receiverToStop != null)
        {
            try
            {
                await receiverToStop.StopAsync();
                // Fully dispose the receiver
                if (receiverToStop is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                Console.WriteLine("Receiver stopped and disposed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping receiver: {ex.Message}");
            }
        }
    }

    public ReceiverStatus GetStatus()
    {
        lock (_lock)
        {
            return new ReceiverStatus
            {
                Mode = _currentMode,
                IsRunning = _currentReceiver != null,
                Config = _currentConfig
            };
        }
    }

    public string? GetCurrentMode()
    {
        lock (_lock)
        {
            return _currentMode;
        }
    }
}

