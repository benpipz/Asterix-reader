namespace AsterixReader.Backend.Services;

public interface IDataReceiverService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
    bool IsRunning { get; }
    event EventHandler<byte[]>? DataReceived;
}


