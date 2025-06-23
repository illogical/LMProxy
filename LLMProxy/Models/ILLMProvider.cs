using Microsoft.SemanticKernel;

namespace LLMProxy.Models;

public interface ILLMProvider
{
    Task<string> SendChatRequestAsync(string serverId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> PingServerAsync(string serverId);
    Task<List<string>> GetAvailableModelsAsync(string serverId);
    /// <summary>
    /// Exposes the server resources (Kernel, HttpClient) for a given serverId.
    /// </summary>
    /// <param name="serverId">The server identifier.</param>
    /// <returns>The tuple of Kernel and HttpClient for the server, or null if not found.</returns>
    Kernel GetKernel(string serverId);
}