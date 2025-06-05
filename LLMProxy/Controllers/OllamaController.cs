using LLMProxy.Helpers;
using LLMProxy.Models;
using LLMProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace LLMProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OllamaController : ControllerBase
    {
        private readonly ILogger<OllamaController> _logger;
        private readonly OllamaService _ollamaSvc;

        public OllamaController(ILogger<OllamaController> logger, OllamaService ollamaSvc)
        {
            _logger = logger;
            _ollamaSvc = ollamaSvc;
        }

        [HttpGet]
        [Route("/")]
        public IActionResult GetApiInfo()
        {
            return Ok("LLMProxy API is running.");
        }

        [HttpGet]
        [Route("/[controller]")]
        public IActionResult GetOllamaInfo()
        {
            return Ok("LLMProxy Ollama API is running.");
        }

        [HttpPost]
        [Route("/[controller]/chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Model) || request.Messages == null || !request.Messages.Any())
            {
                return BadRequest("Invalid chat request.");
            }

            try
            {
                _logger.LogInformation("Received chat request for model: {Model}", request.Model);
                
                var messages = request.Messages.Select(m => new ChatMessage(ChatHelper.GetRoleFromString(m.Role), m.Content)).ToList();
                var response = await _ollamaSvc.ChatWithHistory(request.Model, messages);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}