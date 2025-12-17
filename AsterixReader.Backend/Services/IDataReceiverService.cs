namespace AsterixReader.Backend.Services;

public interface IDataReceiverService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
    event EventHandler<byte[]>? DataReceived;
}


