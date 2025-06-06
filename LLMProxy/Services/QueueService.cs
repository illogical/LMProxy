using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LLMProxy.Services;

public class QueueService
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<QueueService> _logger;

    public QueueService(ILogger<QueueService> logger, IConfiguration configuration)
    {
        _logger = logger;

        _factory = new ConnectionFactory() { 
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost", 
            Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
    }

    public async Task AddToQueue(string queueName, string message)
    {
        if (string.IsNullOrWhiteSpace(queueName) || message == null)
            throw new ArgumentException("Queue name and message must be provided.");

        _logger.LogInformation("AddToQueue request received for queue: {QueueName}", queueName);

        try
        {
            if (queueName.Length > 255)
                throw new ArgumentException("Queue name cannot exceed 255 characters.");

            if (message.Length > 1024 * 1024) // 1 MB limit
                throw new ArgumentException("Message cannot exceed 1 MB.");

            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
            _logger.LogInformation($"Message successfully queued to {queueName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding message to queue {queueName} via host {_factory.HostName} and user {_factory.UserName}");
            throw;
        }

    }
}