using System.Text;
using System.Text.Json;
using AsterixReader.Backend.Hubs;
using AsterixReader.Backend.Models;
using Microsoft.AspNetCore.SignalR;

namespace AsterixReader.Backend.Services;

public class DataProcessingService
{
    private readonly IDataStorageService _storageService;
    private readonly IHubContext<DataHub> _hubContext;

    public DataProcessingService(
        IDataStorageService storageService,
        IHubContext<Hubs.DataHub> hubContext)
    {
        _storageService = storageService;
        _hubContext = hubContext;
    }

    public async Task ProcessDataAsync(byte[] data)
    {
        try
        {
            // Try to deserialize as UTF-8 JSON string first
            string jsonString;
            object? deserializedObject = null;

            try
            {
                jsonString = Encoding.UTF8.GetString(data);
                deserializedObject = JsonSerializer.Deserialize<object>(jsonString);
            }
            catch
            {
                // If not valid UTF-8 JSON, treat as binary data
                // Convert to base64 or create a simple object representation
                jsonString = Convert.ToBase64String(data);
                deserializedObject = new
                {
                    type = "binary",
                    length = data.Length,
                    data = jsonString
                };
            }

            // Create ReceivedData object
            var receivedData = new ReceivedData
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Data = deserializedObject ?? new object(),
                JsonData = JsonSerializer.Serialize(deserializedObject, new JsonSerializerOptions { WriteIndented = true })
            };

            // Store in memory
            _storageService.AddData(receivedData);

            // Notify SignalR hub
            Console.WriteLine($"[DataProcessingService] Sending data via SignalR - ID: {receivedData.Id}, Timestamp: {receivedData.Timestamp}");
            await _hubContext.Clients.All.SendAsync("DataReceived", receivedData);
            Console.WriteLine($"[DataProcessingService] Data sent successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing data: {ex.Message}");
        }
    }
}

