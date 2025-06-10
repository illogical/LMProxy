using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SemanticAspire.ApiService.Services
{
    public class OllamaProvider : ILLMProvider
    {
        private readonly ConcurrentDictionary<string, (Kernel Kernel, HttpClient HttpClient)> _serverResources = new();
        private readonly IHttpClientFactory _httpClientFactory;

        public OllamaProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public void InitializeServer(string serverId, string endpoint, string modelId, float temperature = 0.7f)
        {
            // Create Kernel with Ollama chat completion
            var kernelBuilder = Kernel.CreateBuilder()
                .AddOllamaChatCompletion(modelId, new Uri(endpoint));
            
            var kernel = kernelBuilder.Build();

            // Create HttpClient
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(endpoint);

            _serverResources.TryAdd(serverId, (kernel, httpClient));
        }

        public async Task<string> SendChatRequestAsync(string serverId, ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (!_serverResources.TryGetValue(serverId, out var resources))
                throw new InvalidOperationException($"Server {serverId} not initialized");

            var chatService = resources.Kernel.GetRequiredService<IChatCompletionService>();
            var settings = new OllamaPromptExecutionSettings
            {
                Temperature = request.Temperature,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var chatHistory = ConvertToChatHistory(request.History);
            chatHistory.AddUserMessage(request.Prompt);

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory, 
                executionSettings: settings, 
                kernel: resources.Kernel, 
                cancellationToken: cancellationToken);

            return response.Content ?? string.Empty;
        }

        public async IAsyncEnumerable<string> SendStreamingChatRequestAsync(
            string serverId, 
            ChatRequest request, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_serverResources.TryGetValue(serverId, out var resources))
                throw new InvalidOperationException($"Server {serverId} not initialized");

            var chatService = resources.Kernel.GetRequiredService<IChatCompletionService>();
            var settings = new OllamaPromptExecutionSettings
            {
                Temperature = request.Temperature,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var chatHistory = ConvertToChatHistory(request.History);
            chatHistory.AddUserMessage(request.Prompt);

            await foreach (var content in chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings: settings,
                kernel: resources.Kernel,
                cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(content.Content))
                    yield return content.Content;
            }
        }

        public async Task<bool> PingServerAsync(string serverId)
        {
            if (!_serverResources.TryGetValue(serverId, out var resources))
                return false;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var response = await resources.HttpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, "/"), 
                    cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetAvailableModelsAsync(string serverId)
        {
            if (!_serverResources.TryGetValue(serverId, out var resources))
                throw new InvalidOperationException($"Server {serverId} not initialized");

            try
            {
                var models = await OllamaClient.ListModelsAsync(resources.HttpClient);
                return models.Models.Select(m => m.Name).OrderBy(name => name).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching models for server {serverId}", ex);
            }
        }

        private static ChatHistory ConvertToChatHistory(List<ChatMessage> messages)
        {
            var chatHistory = new ChatHistory();
            
            foreach (var message in messages)
            {
                switch (message.Role.ToLowerInvariant())
                {
                    case "user":
                        chatHistory.AddUserMessage(message.Content);
                        break;
                    case "assistant":
                        chatHistory.AddAssistantMessage(message.Content);
                        break;
                    case "system":
                        chatHistory.AddSystemMessage(message.Content);
                        break;
                }
            }

            return chatHistory;
        }

        public void Dispose()
        {
            foreach (var resource in _serverResources.Values)
            {
                resource.HttpClient?.Dispose();
            }
            _serverResources.Clear();
        }
    }
}