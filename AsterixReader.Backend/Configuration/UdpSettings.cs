namespace AsterixReader.Backend.Configuration;

public class UdpSettings
{
    public int Port { get; set; } = 5000;
    public string ListenAddress { get; set; } = "0.0.0.0";
    public int BufferSize { get; set; } = 65507;
}

