using Microsoft.Extensions.AI;

namespace LLMProxy.Helpers
{
    public static class ChatHelper
    {
        public static ChatRole GetRoleFromString(string role)
        {
            return role.ToLower() switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                "tool" => ChatRole.Tool,
                _ => throw new ArgumentException($"Unknown role: {role}")
            };
        }
    }
}