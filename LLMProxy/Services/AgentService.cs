
using LLMProxy.Models;
using Microsoft.SemanticKernel;

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

    public async Task<string> LoadPromptAsync(string filename)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));
            }

            _logger.LogInformation("Loading prompt from file: {Filename}", filename);

            var promptTemplateText = await File.ReadAllTextAsync(filename);

            // TODO: Pass in the serverId or use a default one
            var kernel = _llmProvider.GetKernel("local");

            if (kernel == null)
            {
                throw new InvalidOperationException("Kernel is not initialized.");
            }

            var promptTemplateFactory = new KernelPromptTemplateFactory();

            var promptTemplate = await promptTemplateFactory.Create(new PromptTemplateConfig(promptTemplateText)).RenderAsync(kernel);

            return promptTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading the prompt.");
            throw new InvalidOperationException("Internal server error", ex);
        }
    }
}
