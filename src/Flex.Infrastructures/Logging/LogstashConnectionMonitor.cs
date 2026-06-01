using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Flex.Infrastructures.Logging
{
    internal sealed class LogstashConnectionMonitor : BackgroundService
    {
        private readonly LogstashLoggingOptions _options;
        private readonly ILogger<LogstashConnectionMonitor> _logger;
        private bool? _connected;

        public LogstashConnectionMonitor(
            LogstashLoggingOptions options,
            ILogger<LogstashConnectionMonitor> logger)
        {
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Uri))
            {
                return;
            }

            if (!TryGetPrimaryEndpoint(_options.Uri, out var endpoint) || endpoint == null)
            {
                _logger.LogWarning(
                    "Logstash logging monitor disabled because endpoint configuration is invalid. Endpoint={Endpoint}",
                    _options.Uri);
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckConnectionAsync(endpoint, stoppingToken);

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(Math.Max(1, _options.HealthCheckIntervalSeconds)),
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task CheckConnectionAsync(Uri endpoint, CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.HealthCheckTimeoutSeconds)));

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(endpoint.Host, endpoint.Port, timeoutCts.Token);
                LogConnected(endpoint);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                LogDisconnected(endpoint, $"timeout after {_options.HealthCheckTimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                LogDisconnected(endpoint, ex.Message, ex);
            }
        }

        private void LogConnected(Uri endpoint)
        {
            if (_connected == true)
            {
                return;
            }

            if (_connected == false)
            {
                _logger.LogInformation("Logstash logging endpoint reconnected. Endpoint={Endpoint}", endpoint);
            }
            else
            {
                _logger.LogInformation("Logstash logging endpoint connected. Endpoint={Endpoint}", endpoint);
            }

            _connected = true;
        }

        private void LogDisconnected(Uri endpoint, string reason, Exception? exception = null)
        {
            if (_connected == false)
            {
                return;
            }

            if (_connected == true)
            {
                _logger.LogWarning(
                    exception,
                    "Logstash logging endpoint disconnected. Endpoint={Endpoint}, Reason={Reason}",
                    endpoint,
                    reason);
            }
            else
            {
                _logger.LogWarning(
                    "Cannot connect to Logstash logging endpoint. Endpoint={Endpoint}, Reason={Reason}",
                    endpoint,
                    reason);
            }

            _connected = false;
        }

        private static bool TryGetPrimaryEndpoint(string value, out Uri? endpoint)
        {
            endpoint = null;

            var uri = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            return !string.IsNullOrWhiteSpace(uri)
                && Uri.TryCreate(uri, UriKind.Absolute, out endpoint)
                && endpoint.Port > 0;
        }
    }
}
