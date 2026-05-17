using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace Flex.Infrastructures.Logging
{
    internal sealed class ElasticLoggingConnectionMonitor : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ElasticLoggingOptions _options;
        private readonly ILogger<ElasticLoggingConnectionMonitor> _logger;
        private bool? _connected;

        public ElasticLoggingConnectionMonitor(
            IHttpClientFactory httpClientFactory,
            ElasticLoggingOptions options,
            ILogger<ElasticLoggingConnectionMonitor> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.NodeUris))
            {
                return;
            }

            if (!TryGetPrimaryEndpoint(_options.NodeUris, out var endpoint) || endpoint == null)
            {
                _logger.LogWarning(
                    "Elasticsearch logging monitor disabled because endpoint configuration is invalid. Endpoint={Endpoint}",
                    _options.NodeUris);
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
                var client = _httpClientFactory.CreateClient(ElasticLoggingExtensions.HttpClientName);
                using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    var credentials = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }

                using var response = await client.SendAsync(request, timeoutCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    LogConnected(endpoint);
                    return;
                }

                LogDisconnected(endpoint, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
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
                _logger.LogInformation("Elasticsearch logging endpoint reconnected. Endpoint={Endpoint}", endpoint);
            }
            else
            {
                _logger.LogInformation("Elasticsearch logging endpoint connected. Endpoint={Endpoint}", endpoint);
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
                    "Elasticsearch logging endpoint disconnected. Endpoint={Endpoint}, Reason={Reason}",
                    endpoint,
                    reason);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Cannot connect to Elasticsearch logging endpoint. Endpoint={Endpoint}, Reason={Reason}",
                    endpoint,
                    reason);
            }

            _connected = false;
        }

        private static bool TryGetPrimaryEndpoint(string nodeUris, out Uri? endpoint)
        {
            endpoint = null;

            var value = nodeUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            return !string.IsNullOrWhiteSpace(value)
                && Uri.TryCreate(value, UriKind.Absolute, out endpoint);
        }
    }
}
