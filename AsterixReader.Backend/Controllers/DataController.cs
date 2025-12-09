using System.Text;
using System.Text.Json;
using AsterixReader.Backend.Models;
using AsterixReader.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AsterixReader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDataStorageService _storageService;
    private readonly DataProcessingService _dataProcessingService;
    private static readonly Random _random = new();

    public DataController(
        IDataStorageService storageService,
        DataProcessingService dataProcessingService)
    {
        _storageService = storageService;
        _dataProcessingService = dataProcessingService;
    }

    [HttpGet]
    public ActionResult<List<ReceivedData>> GetAllData()
    {
        return Ok(_storageService.GetAllData());
    }

    [HttpGet("{id}")]
    public ActionResult<ReceivedData> GetDataById(Guid id)
    {
        var data = _storageService.GetDataById(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    [HttpGet("count")]
    public ActionResult<int> GetCount()
    {
        return Ok(_storageService.GetCount());
    }

    [HttpDelete]
    public IActionResult ClearData()
    {
        _storageService.ClearData();
        return NoContent();
    }

    /// <summary>
    /// Send custom JSON data to be processed and broadcast to all connected clients
    /// </summary>
    /// <param name="jsonData">Any valid JSON object</param>
    /// <returns>Success message with the processed data</returns>
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendData([FromBody] object jsonData)
    {
        try
        {
            var jsonString = JsonSerializer.Serialize(jsonData);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _dataProcessingService.ProcessDataAsync(jsonBytes);
            
            return Ok(new { 
                message = "Data received and processed successfully",
                data = jsonData,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                message = "Error processing data", 
                error = ex.Message 
            });
        }
    }

    [HttpPost("send/raw")]
    [Consumes("application/json")]
    public async Task<IActionResult> SendRawData([FromBody] string jsonString)
    {
        try
        {
            // Validate JSON
            try
            {
                JsonSerializer.Deserialize<JsonElement>(jsonString);
            }
            catch
            {
                return BadRequest(new { 
                    message = "Invalid JSON string",
                    error = "The provided string is not valid JSON"
                });
            }
            
            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _dataProcessingService.ProcessDataAsync(jsonBytes);
            
            return Ok(new { 
                message = "Raw JSON data received and processed successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                message = "Error processing raw data", 
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// Generate a single mock data entry with configurable hierarchy depth
    /// </summary>
    /// <param name="depth">The depth of nested hierarchy (default: 4, max: 10)</param>
    /// <returns>Success message with the generated data</returns>
    [HttpPost("mock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateMockData([FromQuery] int depth = 4)
    {
        if (depth < 1 || depth > 10)
        {
            return BadRequest(new { message = "Depth must be between 1 and 10" });
        }

        var mockData = GenerateNestedMockData(depth);
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mockData));
        await _dataProcessingService.ProcessDataAsync(jsonBytes);
        
        return Ok(new { message = "Mock data generated", data = mockData });
    }


    /// <summary>
    /// Generates nested mock data with configurable depth
    /// </summary>
    /// <param name="maxDepth">Maximum depth of nesting (default: 4)</param>
    /// <returns>A nested object structure</returns>
    private object GenerateNestedMockData(int maxDepth = 4)
    {
        return GenerateNestedObject(maxDepth, 0);
    }

    /// <summary>
    /// Recursively generates a nested object structure
    /// </summary>
    /// <param name="maxDepth">Maximum depth allowed</param>
    /// <param name="currentDepth">Current depth in recursion</param>
    /// <returns>A nested object or primitive value</returns>
    private object GenerateNestedObject(int maxDepth, int currentDepth)
    {
        // At max depth, return primitive values
        if (currentDepth >= maxDepth)
        {
            return GetRandomPrimitive();
        }

        // Generate a dictionary with random properties
        var result = new Dictionary<string, object>();
        var propertyCount = _random.Next(3, 8); // 3-7 properties per level

        for (int i = 0; i < propertyCount; i++)
        {
            var key = $"property{currentDepth}_{i + 1}";
            
            // Decide whether to nest further or use primitive
            var shouldNest = currentDepth < maxDepth - 1 && _random.NextDouble() > 0.3; // 70% chance to nest if not at max
            
            if (shouldNest)
            {
                result[key] = GenerateNestedObject(maxDepth, currentDepth + 1);
            }
            else
            {
                result[key] = GetRandomPrimitive();
            }
        }

        // Add some metadata
        result["level"] = currentDepth;
        result["timestamp"] = DateTime.UtcNow.ToString("O");
        
        return result;
    }

    /// <summary>
    /// Generates a random primitive value (string, number, boolean, or null)
    /// </summary>
    private object GetRandomPrimitive()
    {
        var type = _random.Next(4);
        return type switch
        {
            0 => $"value_{_random.Next(1000, 9999)}",
            1 => Math.Round(_random.NextDouble() * 1000, 2),
            2 => _random.Next(2) == 1,
            _ => "null"
        };
    }
}
