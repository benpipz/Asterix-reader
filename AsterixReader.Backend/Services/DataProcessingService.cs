using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Collections.Generic;
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
        IHubContext<DataHub> hubContext)
    {
        _storageService = storageService;
        _hubContext = hubContext;
    }

    private object EnsureMetadataField(object? deserializedObject)
    {
        if (deserializedObject == null)
        {
            return new { metadata = "Empty message" };
        }

        try
        {
            // Convert to JsonNode to work with dynamic structure
            var jsonString = JsonSerializer.Serialize(deserializedObject);
            var jsonNode = JsonNode.Parse(jsonString);

            if (jsonNode is JsonObject jsonObj)
            {
                // Check if metadata field already exists
                if (!jsonObj.ContainsKey("metadata") && !jsonObj.ContainsKey("Metadata") && !jsonObj.ContainsKey("METADATA"))
                {
                    // Generate metadata based on the data structure
                    var metadata = GenerateMetadata(jsonObj);
                    jsonObj["metadata"] = metadata;
                }
                
                return jsonObj;
            }
            
            // If it's not a JSON object, wrap it with metadata
            return new
            {
                data = deserializedObject,
                metadata = GenerateMetadataForValue(deserializedObject)
            };
        }
        catch
        {
            // If parsing fails, wrap the object with metadata
            return new
            {
                data = deserializedObject,
                metadata = "Message data"
            };
        }
    }

    private string GenerateMetadata(JsonObject jsonObj)
    {
        var parts = new List<string>();

        // Try to extract meaningful information for metadata
        if (jsonObj.TryGetPropertyValue("type", out var typeNode))
        {
            parts.Add($"Type: {typeNode?.ToString()}");
        }

        if (jsonObj.TryGetPropertyValue("message", out var messageNode))
        {
            var message = messageNode?.ToString();
            if (!string.IsNullOrEmpty(message) && message.Length > 50)
            {
                message = message.Substring(0, 50) + "...";
            }
            parts.Add(message ?? "");
        }
        else if (jsonObj.TryGetPropertyValue("eventType", out var eventTypeNode))
        {
            parts.Add($"Event: {eventTypeNode?.ToString()}");
        }

        // Count properties
        var propertyCount = jsonObj.Count;
        if (propertyCount > 0)
        {
            parts.Add($"{propertyCount} {(propertyCount == 1 ? "property" : "properties")}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "JSON message";
    }

    private string GenerateMetadataForValue(object? value)
    {
        if (value == null)
        {
            return "Null value";
        }

        var valueType = value.GetType();
        
        if (valueType == typeof(string))
        {
            var str = value.ToString() ?? "";
            return str.Length > 50 ? str.Substring(0, 50) + "..." : str;
        }

        if (valueType.IsPrimitive || valueType == typeof(decimal))
        {
            return $"Value: {value}";
        }

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var count = enumerable.Cast<object>().Count();
            return $"Array/Collection with {count} item{(count == 1 ? "" : "s")}";
        }

        return $"Object of type {valueType.Name}";
    }

    public async Task ProcessDataAsync(byte[] data)
    {
        try
        {
            // Try to deserialize as UTF-8 JSON string first
            object? deserializedObject = null;

            try
            {
                var jsonString = Encoding.UTF8.GetString(data);
                deserializedObject = JsonSerializer.Deserialize<object>(jsonString);
            }
            catch
            {
                // If not valid UTF-8 JSON, treat as binary data
                // Convert to base64 or create a simple object representation
                var base64String = Convert.ToBase64String(data);
                deserializedObject = new
                {
                    type = "binary",
                    length = data.Length,
                    data = base64String,
                    metadata = $"Binary data packet ({data.Length} bytes)"
                };
            }

            // Ensure metadata field exists in the deserialized object
            deserializedObject = EnsureMetadataField(deserializedObject);

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

