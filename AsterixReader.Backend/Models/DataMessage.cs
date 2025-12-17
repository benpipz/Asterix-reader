namespace AsterixReader.Backend.Models;

public class DataMessage
{
    public ReceivedData Data { get; set; } = new();
    public string MessageType { get; set; } = "DataReceived";
}


