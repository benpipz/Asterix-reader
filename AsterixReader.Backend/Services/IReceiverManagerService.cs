using AsterixReader.Backend.Configuration;
using AsterixReader.Backend.Models;

namespace AsterixReader.Backend.Services;

public interface IReceiverManagerService
{
    Task StartUdpReceiverAsync(UdpReceiverConfig config, CancellationToken cancellationToken);
    Task StartPcapReceiverAsync(PcapReceiverConfig config, CancellationToken cancellationToken);
    Task StopReceiverAsync();
    ReceiverStatus GetStatus();
    string? GetCurrentMode();
}

