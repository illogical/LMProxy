namespace LLMProxy.Models;

public interface ILLMProvider
{
    Task<string> SendChatRequestAsync(string serverId, ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> SendStreamingChatRequestAsync(string serverId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> PingServerAsync(string serverId);
    Task<List<string>> GetAvailableModelsAsync(string serverId);
}