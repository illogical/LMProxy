using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using SemanticAspire.ApiService.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;

namespace LLMProxy.Services;

public class MultiServerChatManager
{
    private readonly ConcurrentDictionary<string, ServerConfig> _servers = new();
    private readonly ConcurrentDictionary<string, bool> _serverBusyStatus = new();
    private readonly SemaphoreSlim _serverSelectionLock = new(1);
    private readonly ILLMProvider _llmProvider;

    public MultiServerChatManager(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    public void AddServer(ServerConfig config)
    {
        if (string.IsNullOrEmpty(config.ServerId))
            throw new ArgumentException("ServerId cannot be null or empty", nameof(config));

        _servers.TryAdd(config.ServerId, config);
        _serverBusyStatus.TryAdd(config.ServerId, false);
    }

    public void RemoveServer(string serverId)
    {
        _servers.TryRemove(serverId, out _);
        _serverBusyStatus.TryRemove(serverId, out _);
    }

    public async Task<string?> GetNextAvailableServerAsync()
    {
        await _serverSelectionLock.WaitAsync();
        try
        {
            var availableServer = _servers
                .Where(s => s.Value.IsAvailable && !_serverBusyStatus.GetValueOrDefault(s.Key, false))
                .OrderBy(s => s.Value.LastUsed)
                .FirstOrDefault();

            if (availableServer.Key == null)
                return null;

            availableServer.Value.LastUsed = DateTime.UtcNow;
            return availableServer.Key;
        }
        finally
        {
            _serverSelectionLock.Release();
        }
    }

    public async Task<ChatResponse> SendChatRequestAsync(string serverId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!_servers.TryGetValue(serverId, out var config))
        {
            return new ChatResponse
            {
                ServerId = serverId,
                IsSuccess = false,
                ErrorMessage = $"Server {serverId} not found"
            };
        }

        if (!config.IsAvailable)
        {
            return new ChatResponse
            {
                ServerId = serverId,
                ModelId = config.ModelId,
                IsSuccess = false,
                ErrorMessage = $"Server {serverId} is not available"
            };
        }

        // Mark server as busy
        _serverBusyStatus.TryUpdate(serverId, true, false);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _llmProvider.SendChatRequestAsync(serverId, request, cancellationToken);
            stopwatch.Stop();

            return new ChatResponse
            {
                ServerId = serverId,
                ModelId = config.ModelId,
                Response = response,
                Duration = stopwatch.Elapsed,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ChatResponse
            {
                ServerId = serverId,
                ModelId = config.ModelId,
                Duration = stopwatch.Elapsed,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            // Mark server as available
            _serverBusyStatus.TryUpdate(serverId, false, true);
        }
    }

    public async Task<ChatResponse> SendChatRequestToNextAvailableServerAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var serverId = await GetNextAvailableServerAsync();
        if (serverId == null)
        {
            return new ChatResponse
            {
                IsSuccess = false,
                ErrorMessage = "No available servers"
            };
        }

        return await SendChatRequestAsync(serverId, request, cancellationToken);
    }

    public async Task<Dictionary<string, ChatResponse>> SendChatRequestToAllServersAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var tasks = _servers.Keys.Select(async serverId =>
        {
            var response = await SendChatRequestAsync(serverId, request, cancellationToken);
            return new KeyValuePair<string, ChatResponse>(serverId, response);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async IAsyncEnumerable<ChatResponse> SendChatRequestToAllServersStreamingAsync(
        ChatRequest request, 
        CancellationToken cancellationToken = default)
    {
        var tasks = _servers.Keys.Select(serverId => 
            SendChatRequestAsync(serverId, request, cancellationToken)).ToList();

        while (tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            yield return await completedTask;
        }
    }

    public void SetServerAvailability(string serverId, bool isAvailable)
    {
        if (_servers.TryGetValue(serverId, out var config))
        {
            config.IsAvailable = isAvailable;
        }
    }

    public bool IsServerBusy(string serverId)
    {
        return _serverBusyStatus.GetValueOrDefault(serverId, false);
    }

    public bool IsServerAvailable(string serverId)
    {
        return _servers.TryGetValue(serverId, out var config) && 
                config.IsAvailable && 
                !_serverBusyStatus.GetValueOrDefault(serverId, false);
    }

    public IEnumerable<ServerConfig> GetAvailableServers()
    {
        return _servers.Values
            .Where(s => s.IsAvailable && !_serverBusyStatus.GetValueOrDefault(s.ServerId, false))
            .ToList();
    }

    public IEnumerable<ServerConfig> GetAllServers()
    {
        return _servers.Values.ToList();
    }

    public async Task<bool> PingServerAsync(string serverId)
    {
        if (!_servers.ContainsKey(serverId))
            return false;

        try
        {
            return await _llmProvider.PingServerAsync(serverId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetAvailableModelsAsync(string serverId)
    {
        if (!_servers.ContainsKey(serverId))
            throw new InvalidOperationException($"Server {serverId} not found");

        return await _llmProvider.GetAvailableModelsAsync(serverId);
    }
}