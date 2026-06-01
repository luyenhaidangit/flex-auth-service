using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Flex.Infrastructures.Logging
{
    internal sealed class LogstashHttpSink : ILogEventSink, IDisposable
    {
        private static readonly HttpClient HttpClient = new();

        private readonly JsonFormatter _formatter = new(renderMessage: true);
        private readonly Uri _endpoint;
        private readonly Channel<string> _channel;
        private readonly CancellationTokenSource _stopping = new();
        private readonly Task _worker;

        public LogstashHttpSink(Uri endpoint, int queueCapacity)
        {
            _endpoint = endpoint;
            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(Math.Max(1, queueCapacity))
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
            _worker = Task.Run(ProcessQueueAsync);
        }

        public void Emit(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            _channel.Writer.TryWrite(writer.ToString());
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();
            _stopping.Cancel();

            try
            {
                _worker.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Logging must not block application shutdown.
            }

            _stopping.Dispose();
        }

        private async Task ProcessQueueAsync()
        {
            await foreach (var payload in _channel.Reader.ReadAllAsync(_stopping.Token))
            {
                try
                {
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await HttpClient.PostAsync(_endpoint, content, _stopping.Token);
                    response.EnsureSuccessStatusCode();
                }
                catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // Development-only sink: drop failed events instead of impacting request handling.
                }
            }
        }
    }
}
