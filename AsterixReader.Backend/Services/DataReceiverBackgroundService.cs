using AsterixReader.Backend.Services;

namespace AsterixReader.Backend.Services;

public class DataReceiverBackgroundService : BackgroundService
{
    private readonly IDataReceiverService _dataReceiver;
    private readonly DataProcessingService _dataProcessor;
    private readonly ILogger<DataReceiverBackgroundService> _logger;

    public DataReceiverBackgroundService(
        IDataReceiverService dataReceiver,
        IServiceProvider serviceProvider,
        ILogger<DataReceiverBackgroundService> logger)
    {
        _dataReceiver = dataReceiver;
        _dataProcessor = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DataProcessingService>();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wire up the event handler
        _dataReceiver.DataReceived += async (sender, data) =>
        {
            await _dataProcessor.ProcessDataAsync(data);
        };

        // Start the receiver
        await _dataReceiver.StartAsync(stoppingToken);
        _logger.LogInformation("UDP Data Receiver started");

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _dataReceiver.StopAsync();
        _logger.LogInformation("UDP Data Receiver stopped");
        await base.StopAsync(cancellationToken);
    }
}

