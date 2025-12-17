using AsterixReader.Backend.Configuration;
using AsterixReader.Backend.Models;
using AsterixReader.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AsterixReader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceiverController : ControllerBase
{
    private readonly IReceiverManagerService _receiverManager;
    private readonly ILogger<ReceiverController> _logger;

    public ReceiverController(
        IReceiverManagerService receiverManager,
        ILogger<ReceiverController> logger)
    {
        _receiverManager = receiverManager;
        _logger = logger;
    }

    [HttpPost("udp/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartUdpReceiver([FromBody] UdpReceiverConfig config, CancellationToken cancellationToken)
    {
        try
        {
            // Validate configuration
            if (config.Port < 1 || config.Port > 65535)
            {
                return BadRequest(new { message = "Port must be between 1 and 65535" });
            }

            if (string.IsNullOrEmpty(config.ListenAddress))
            {
                return BadRequest(new { message = "Listen address is required" });
            }

            if (config.JoinMulticastGroup && string.IsNullOrEmpty(config.MulticastAddress))
            {
                return BadRequest(new { message = "Multicast address is required when joining multicast group" });
            }

            if (config.JoinMulticastGroup && !string.IsNullOrEmpty(config.MulticastAddress))
            {
                if (!System.Net.IPAddress.TryParse(config.MulticastAddress, out var multicastIp))
                {
                    return BadRequest(new { message = "Invalid multicast address format" });
                }

                // Validate multicast IP range (224.0.0.0 to 239.255.255.255)
                var firstOctet = multicastIp.GetAddressBytes()[0];
                if (firstOctet < 224 || firstOctet > 239)
                {
                    return BadRequest(new { message = "Multicast address must be in range 224.0.0.0 to 239.255.255.255" });
                }
            }

            await _receiverManager.StartUdpReceiverAsync(config, cancellationToken);
            return Ok(new { message = "UDP receiver started successfully", config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting UDP receiver");
            return BadRequest(new { message = "Failed to start UDP receiver", error = ex.Message });
        }
    }

    [HttpPost("pcap/upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPcapFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pcap" && extension != ".pcapng")
            {
                return BadRequest(new { message = "File must be a .pcap or .pcapng file" });
            }

            // Create temp directory if it doesn't exist
            var tempDir = Path.Combine(Path.GetTempPath(), "asterix-reader-pcap");
            Directory.CreateDirectory(tempDir);

            // Generate unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(tempDir, fileName);

            // Save uploaded file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation($"PCAP file uploaded: {filePath} ({file.Length} bytes)");

            return Ok(new { filePath, fileName = file.FileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PCAP file");
            return BadRequest(new { message = "Failed to upload file", error = ex.Message });
        }
    }

    [HttpPost("pcap/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartPcapReceiver([FromBody] PcapReceiverConfig config, CancellationToken cancellationToken)
    {
        try
        {
            // Validate configuration
            if (string.IsNullOrEmpty(config.FilePath))
            {
                return BadRequest(new { message = "File path is required" });
            }

            if (!System.IO.File.Exists(config.FilePath))
            {
                return BadRequest(new { message = $"PCAP file not found: {config.FilePath}" });
            }

            // Validate file extension
            var extension = Path.GetExtension(config.FilePath).ToLowerInvariant();
            if (extension != ".pcap" && extension != ".pcapng")
            {
                return BadRequest(new { message = "File must be a .pcap or .pcapng file" });
            }

            await _receiverManager.StartPcapReceiverAsync(config, cancellationToken);
            return Ok(new { message = "PCAP receiver started successfully", config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting PCAP receiver");
            return BadRequest(new { message = "Failed to start PCAP receiver", error = ex.Message });
        }
    }

    [HttpPost("stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StopReceiver()
    {
        try
        {
            await _receiverManager.StopReceiverAsync();
            return Ok(new { message = "Receiver stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping receiver");
            return BadRequest(new { message = "Failed to stop receiver", error = ex.Message });
        }
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var status = _receiverManager.GetStatus();
        return Ok(status);
    }
}

