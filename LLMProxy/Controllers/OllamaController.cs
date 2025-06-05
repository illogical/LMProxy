using LLMProxy.Services;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult GetOllamaInfo()
        {
            return Ok("Ollama API is running.");
        }

        [HttpGet]
        [Route("/chat")]
        public async Task<IActionResult> Chat()
        {
            try
            {
                var response = await _ollamaSvc.ConversationHistory();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
