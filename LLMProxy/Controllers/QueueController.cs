using LLMProxy.Models;
using LLMProxy.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : ControllerBase
{
    private readonly ILogger<QueueController> _logger;
    private readonly QueueService _queueSvc;

    public QueueController(ILogger<QueueController> logger, QueueService queueService)
    {
        _logger = logger;
        _queueSvc = queueService;
    }
    
    [HttpPost]
    [Route("/[controller]/add/{queue}")]
    public async Task<IActionResult> AddQueue([FromRoute] string queue, [FromBody] QueueAddRequest request)
    {
        if (request == null || request.Message == null || string.IsNullOrWhiteSpace(queue))
        {
            return BadRequest("Invalid queue request.");
        }

        try
        {
            _logger.LogInformation($"Received a queue add request to queue {queue}.");
            
            await _queueSvc.AddToQueue(queue, request.Message);

            _logger.LogInformation($"Message added to queue {queue}: {request.Message}");
            return Ok("Message added to queue {queue}: {request.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing queue add request for queue {queue}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
