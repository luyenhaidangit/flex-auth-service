namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Service to verify RabbitMQ exchange existence before application starts.
    /// </summary>
    public interface IRabbitMQExchangeVerifier
    {
        /// <summary>
        /// Verifies that the required exchange exists.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if exchange exists; otherwise false.</returns>
        Task<bool> VerifyExchangeAsync(CancellationToken cancellationToken = default);
    }
}
