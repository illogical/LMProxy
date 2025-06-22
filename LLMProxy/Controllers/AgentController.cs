using LLMProxy.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMProxy.Controllers;

public class AgentController : ControllerBase
{
    private readonly ILogger<AgentController> _logger;
    private readonly AgentService _agentService;

    public AgentController(ILogger<AgentController> logger, AgentService agentService)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    [Route("/[controller]/{agent}")]
    public async Task<IActionResult> PromptAgent([FromRoute] string agent, [FromBody] string prompt)
    {
        try
        {
            var result = await _agentService.PromptAgentAsync(agent, prompt);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}