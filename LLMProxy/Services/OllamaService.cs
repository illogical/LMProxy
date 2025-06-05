using Microsoft.Extensions.AI;

namespace LLMProxy.Services
{
    public class OllamaService
    {
        public async Task<ChatResponse> ConversationHistory()
        {
            var endpoint = "http://host.docker.internal:11434/";
            var modelId = "gemma3";

            IChatClient client = new OllamaChatClient(endpoint, modelId: modelId);

            List<ChatMessage> conversation =
            [
                new(ChatRole.System, "You are a helpful AI assistant"),
                new(ChatRole.User, "How do I effectively manage LLM context?")
            ];

            return await client.GetResponseAsync(conversation);
        }
    }
}
