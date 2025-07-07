﻿using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace LLMProxy.Services;

public class OllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly string _ollamaApiUrl;

    public OllamaService(ILogger<OllamaService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _ollamaApiUrl = configuration["OllamaApiUrl"] ?? "http://host.docker.internal:11434/";
    }

    public async Task<ChatResponse> Chat(string model, string prompt, string systemPrompt = "")
    {
        IChatClient client = new OllamaChatClient(_ollamaApiUrl, modelId: model);

        List<ChatMessage> conversation = new();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            conversation.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
        conversation.Add(new ChatMessage(ChatRole.User, prompt));

        return await client.GetResponseAsync(conversation);
    }

    public async Task<ChatResponse> ChatWithHistory(string model, List<ChatMessage> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            throw new ArgumentException("Messages cannot be null or empty.", nameof(messages));
        }

        // Ensure the last message is the user prompt
        string prompt = messages.Last().Text;
        {
            IChatClient client = new OllamaChatClient(_ollamaApiUrl, modelId: model);

            return await client.GetResponseAsync(messages);
        }
    }
}
