
using LLMProxy.Models;

namespace LLMProxy.Services;

public class AgentService
{
    private readonly ILogger<AgentService> _logger;
    private readonly ILLMProvider _llmProvider;

    public AgentService(ILogger<AgentService> logger, ILLMProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    public async Task<string> PromptAgentAsync(string agent, string prompt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agent))
            {
                throw new ArgumentException("Agent name cannot be null or empty.", nameof(agent));
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            _logger.LogInformation("Processing prompt for agent: {Agent}", agent);

            var response = await _llmProvider.SendChatRequestAsync(agent, new ChatRequest
            {
                Messages = new List<MessageDto>
                {
                    new MessageDto { Role = "user", Content = prompt }
                }
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the agent prompt.");
            throw new InvalidOperationException("Internal server error", ex);
        }
    }
}