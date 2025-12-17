using AsterixReader.Backend.Models;
using AsterixReader.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AsterixReader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageModeController : ControllerBase
{
    private readonly IMessageModeService _messageModeService;

    public MessageModeController(IMessageModeService messageModeService)
    {
        _messageModeService = messageModeService;
    }

    /// <summary>
    /// Get the current message mode
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetMessageMode()
    {
        return Ok(new { mode = _messageModeService.CurrentMode.ToString() });
    }

    /// <summary>
    /// Update the message mode
    /// </summary>
    /// <param name="mode">The message mode to set (Default, Incoming, or Outgoing)</param>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SetMessageMode([FromBody] SetMessageModeRequest request)
    {
        try
        {
            if (!Enum.TryParse<MessageMode>(request.Mode, ignoreCase: true, out var messageMode))
            {
                return BadRequest(new { 
                    message = "Invalid message mode", 
                    error = $"Mode must be one of: {string.Join(", ", Enum.GetNames(typeof(MessageMode)))}" 
                });
            }

            _messageModeService.SetMode(messageMode);
            return Ok(new { 
                message = "Message mode updated successfully", 
                mode = messageMode.ToString() 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                message = "Error updating message mode", 
                error = ex.Message 
            });
        }
    }
}

public class SetMessageModeRequest
{
    public string Mode { get; set; } = string.Empty;
}

