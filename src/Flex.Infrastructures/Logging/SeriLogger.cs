using Serilog;
using Serilog.Formatting.Json;
using Microsoft.AspNetCore.Builder;
using Flex.Infrastructures.Observability;

namespace Flex.Infrastructures.Logging
{
    public static class SeriLogger
    {
        private const string OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";

        public static void Configure(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var env = builder.Environment;
            var host = builder.Host;

            // Options configuration
            var loggingOptions = SerilogLoggingOptions.Resolve(configuration, env);
            var logstashEndpointIsValid = Uri.TryCreate(loggingOptions.Logstash.Uri, UriKind.Absolute, out var logstashEndpoint);

            // Create logger configuration
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new EcsLogFieldEnricher())
                .Enrich.WithProperty(LogFields.ServiceName, loggingOptions.ServiceName)
                .Enrich.WithProperty(LogFields.ServiceEnvironment, loggingOptions.ServiceEnvironment)
                .Enrich.WithProperty(LogFields.HostName, loggingOptions.HostName)
                .WriteTo.Async(a =>
                {
                    if (loggingOptions.Sinks.Console)
                    {
                        a.Console(outputTemplate: OutputTemplate);
                    }

                    if (loggingOptions.Sinks.File)
                    {
                        a.File(
                            new JsonFormatter(renderMessage: true),
                            path: "logs/log-.json",
                            rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 10_000_000,
                            rollOnFileSizeLimit: true,
                            retainedFileCountLimit: 7,
                            shared: true
                        );
                    }
                }, bufferSize: 5000)
                .WriteTo.Async(a =>
                {
                    if (loggingOptions.Sinks.Logstash && logstashEndpointIsValid)
                    {
                        a.Http(
                            requestUri: logstashEndpoint!.ToString(),
                            queueLimitBytes: loggingOptions.Logstash.QueueLimitBytes,
                            logEventsInBatchLimit: loggingOptions.Logstash.LogEventsInBatchLimit,
                            period: loggingOptions.Logstash.FlushTimeout,
                            textFormatter: new JsonFormatter(renderMessage: true));
                    }

                }, bufferSize: 10000);

            // Create logger
            Log.Logger = loggerConfig.CreateLogger();

            // Use Serilog
            host.UseSerilog();
        }

    }
}
