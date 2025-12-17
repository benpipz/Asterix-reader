namespace AsterixReader.Backend.Configuration;

public class PcapReceiverConfig
{
    public string FilePath { get; set; } = string.Empty;
    public string? Filter { get; set; }
}

