using Microsoft.AspNetCore.Builder;
using Flex.Infrastructures.Observability;
using Serilog;
using Serilog.Formatting.Json;

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

            // Read application name and environment name
            var applicationName = env.ApplicationName?.ToLowerInvariant().Replace('.', '-') ?? "unknown-application";
            var environmentName = string.IsNullOrWhiteSpace(env.EnvironmentName)
                ? "development"
                : env.EnvironmentName.Trim().ToLowerInvariant();

            var sinkOptions = LoggingSinkOptions.Resolve(configuration);
            var logstashOptions = LogstashLoggingOptions.Resolve(configuration);

            var serviceName = applicationName;
            Uri? logstashEndpoint = null;
            var logstashEndpointIsValid =
                !string.IsNullOrWhiteSpace(logstashOptions.Uri)
                && Uri.TryCreate(logstashOptions.Uri, UriKind.Absolute, out logstashEndpoint);

            // Create logger configuration
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new EcsLogEventEnricher())
                .Enrich.WithProperty(LogFields.ServiceName, serviceName)
                .Enrich.WithProperty(LogFields.ServiceEnvironment, environmentName)
                .Enrich.WithProperty(LogFields.HostName, Environment.MachineName)
                .WriteTo.Async(a =>
                {
                    if (sinkOptions.Console)
                    {
                        a.Console(outputTemplate: OutputTemplate);
                    }

                    if (sinkOptions.File)
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
                    if (sinkOptions.Logstash && logstashEndpointIsValid && logstashEndpoint != null)
                    {
                        a.Sink(new LogstashHttpSink(logstashEndpoint, logstashOptions.QueueCapacity));
                    }

                }, bufferSize: 10000);

            // Create logger
            Log.Logger = loggerConfig.CreateLogger();

            if (sinkOptions.Logstash && !string.IsNullOrWhiteSpace(logstashOptions.Uri) && !logstashEndpointIsValid)
            {
                Log.Warning(
                    "Logstash logging sink disabled because endpoint configuration is invalid. Endpoint={Endpoint}",
                    logstashOptions.Uri);
            }

            // Use Serilog
            host.UseSerilog();
        }

    }
}
