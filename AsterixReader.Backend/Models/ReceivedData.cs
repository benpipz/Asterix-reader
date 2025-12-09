using System.Text.Json.Serialization;

namespace AsterixReader.Backend.Models;

public class ReceivedData
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("data")]
    public object Data { get; set; } = new();
    
    [JsonPropertyName("jsonData")]
    public string JsonData { get; set; } = string.Empty;
}

