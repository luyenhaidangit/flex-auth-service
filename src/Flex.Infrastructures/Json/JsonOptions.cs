using System.Text.Json;

namespace Flex.Infrastructures.Json
{
    /// <summary>
    /// Shared JSON serializer options for domain events serialization/deserialization.
    /// Ensures consistency across OutboxWriter, OutboxProcessor, and RabbitMQPublisher.
    /// </summary>
    public static class JsonOptions
    {
        /// <summary>
        /// JSON serializer options for domain events.
        /// Uses camelCase naming policy and case-insensitive property matching for deserialization.
        /// </summary>
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }
}
