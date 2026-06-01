using Microsoft.AspNetCore.Builder;
using Flex.Infrastructures.Observability;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenSearch;

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

            // Bind Elastic logging options from configuration
            var elasticOptions = ElasticLoggingOptionsResolver.Resolve(configuration);
            var logstashOptions = LogstashLoggingOptionsResolver.Resolve(configuration);

            var elasticUri = elasticOptions.NodeUris;
            var username = elasticOptions.Username;
            var password = elasticOptions.Password;
            var serviceName = string.IsNullOrWhiteSpace(elasticOptions.ServiceName) ? applicationName : elasticOptions.ServiceName.Trim().ToLowerInvariant();
            var elasticIndexFormat = $"{elasticOptions.IndexPrefix}-{serviceName}-{{0:yyyy.MM.dd}}";
            var elasticEndpointIsValid = TryGetPrimaryEndpoint(elasticUri, out var elasticEndpoint);
            var logstashEndpointIsValid = TryGetPrimaryEndpoint(logstashOptions.Uri, out var logstashEndpoint);

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
                    a.Console(outputTemplate: OutputTemplate);
                    a.File(
                        new JsonFormatter(renderMessage: true),
                        path: "logs/log-.json",
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 10_000_000,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 7,
                        shared: true
                    );
                }, bufferSize: 5000)
                .WriteTo.Async(a =>
                {
                    if (logstashOptions.Enabled && logstashEndpointIsValid && logstashEndpoint != null)
                    {
                        a.Sink(new LogstashHttpSink(logstashEndpoint, logstashOptions.QueueCapacity));
                    }

                    if (elasticOptions.Enabled && elasticEndpointIsValid && elasticEndpoint != null)
                    {
                        a.OpenSearch(new OpenSearchSinkOptions(elasticEndpoint)
                        {
                            IndexFormat = elasticIndexFormat,
                            InlineFields = true,
                            AutoRegisterTemplate = false,
                            ModifyConnectionSettings = c => c.BasicAuthentication(username, password),
                            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
                        });
                    }
                }, bufferSize: 10000);

            // Create logger
            Log.Logger = loggerConfig.CreateLogger();

            if (elasticOptions.Enabled && !string.IsNullOrWhiteSpace(elasticUri) && !elasticEndpointIsValid)
            {
                Log.Warning(
                    "Elasticsearch logging sink disabled because endpoint configuration is invalid. Endpoint={Endpoint}",
                    elasticUri);
            }

            if (logstashOptions.Enabled && !string.IsNullOrWhiteSpace(logstashOptions.Uri) && logstashEndpointIsValid && logstashEndpoint != null)
            {
                Log.Information("Logstash logging sink enabled. Endpoint={Endpoint}", logstashEndpoint);
            }

            if (logstashOptions.Enabled && !string.IsNullOrWhiteSpace(logstashOptions.Uri) && !logstashEndpointIsValid)
            {
                Log.Warning(
                    "Logstash logging sink disabled because endpoint configuration is invalid. Endpoint={Endpoint}",
                    logstashOptions.Uri);
            }

            // Use Serilog
            host.UseSerilog();
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
