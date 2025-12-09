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
    private static CancellationTokenSource? _continuousMockCancellation;

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
    /// Generate multiple mock data entries in batch
    /// </summary>
    /// <param name="count">Number of entries to generate (default: 5, max: 100)</param>
    /// <param name="depth">The depth of nested hierarchy for each entry (default: 4, max: 10)</param>
    /// <returns>Success message with the count of generated entries</returns>
    [HttpPost("mock/batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateBatchMockData([FromQuery] int count = 5, [FromQuery] int depth = 4)
    {
        if (count < 1 || count > 100)
        {
            return BadRequest(new { message = "Count must be between 1 and 100" });
        }
        
        if (depth < 1 || depth > 10)
        {
            return BadRequest(new { message = "Depth must be between 1 and 10" });
        }

        for (int i = 0; i < count; i++)
        {
            var mockData = GenerateNestedMockData(depth);
            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mockData));
            await _dataProcessingService.ProcessDataAsync(jsonBytes);
            await Task.Delay(100);
        }

        return Ok(new { message = $"Generated {count} mock data entries", count });
    }

    /// <summary>
    /// Start continuous mock data generation
    /// </summary>
    /// <param name="interval">Interval between generations in milliseconds (default: 2000, min: 500, max: 60000)</param>
    /// <param name="depth">The depth of nested hierarchy for each entry (default: 4, max: 10)</param>
    /// <returns>Success message with generation details</returns>
    [HttpPost("mock/continuous")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartContinuousMockData([FromQuery] int interval = 2000, [FromQuery] int depth = 4)
    {
        if (interval < 500 || interval > 60000)
        {
            return BadRequest(new { message = "Interval must be between 500ms and 60000ms" });
        }
        
        if (depth < 1 || depth > 10)
        {
            return BadRequest(new { message = "Depth must be between 1 and 10" });
        }

        _continuousMockCancellation?.Cancel();
        _continuousMockCancellation = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!_continuousMockCancellation.Token.IsCancellationRequested)
            {
                var mockData = GenerateNestedMockData(depth);
                var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mockData));
                await _dataProcessingService.ProcessDataAsync(jsonBytes);
                await Task.Delay(interval, _continuousMockCancellation.Token);
            }
        }, _continuousMockCancellation.Token);

        return Ok(new { message = $"Started continuous mock data generation (interval: {interval}ms, depth: {depth})" });
    }

    /// <summary>
    /// Stop continuous mock data generation
    /// </summary>
    [HttpPost("mock/continuous/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult StopContinuousMockData()
    {
        if (_continuousMockCancellation == null || _continuousMockCancellation.Token.IsCancellationRequested)
        {
            return BadRequest(new { message = "Continuous mock data generation is not running" });
        }

        _continuousMockCancellation.Cancel();
        _continuousMockCancellation = null;
        return Ok(new { message = "Stopped continuous mock data generation" });
    }

    /// <summary>
    /// Send custom JSON data as mock data to be processed and broadcast to all connected clients
    /// </summary>
    /// <param name="jsonData">Any valid JSON object</param>
    /// <returns>Success message with the processed data</returns>
    [HttpPost("mock/custom")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendCustomMockData([FromBody] JsonElement jsonData)
    {
        try
        {
            var jsonString = jsonData.GetRawText();
            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _dataProcessingService.ProcessDataAsync(jsonBytes);
            
            return Ok(new { 
                message = "Custom mock data received and processed successfully",
                data = JsonSerializer.Deserialize<object>(jsonString),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                message = "Error processing custom mock data", 
                error = ex.Message 
            });
        }
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
