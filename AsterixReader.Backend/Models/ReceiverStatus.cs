namespace AsterixReader.Backend.Models;

public class ReceiverStatus
{
    public string? Mode { get; set; } // "UDP", "PCAP", or null
    public bool IsRunning { get; set; }
    public object? Config { get; set; } // Current configuration
}

