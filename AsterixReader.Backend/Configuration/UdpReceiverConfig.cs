namespace AsterixReader.Backend.Configuration;

public class UdpReceiverConfig
{
    public int Port { get; set; }
    public string ListenAddress { get; set; } = "0.0.0.0";
    public bool JoinMulticastGroup { get; set; }
    public string? MulticastAddress { get; set; }
}

