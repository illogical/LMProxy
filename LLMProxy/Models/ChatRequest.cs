

namespace LLMProxy.Models;

public class ChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<MessageDto> Messages { get; set; } = new();
    public bool Stream { get; set; } = false;
    public float Temperature { get; set; } = 0.1f;
}
