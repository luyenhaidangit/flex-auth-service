using Microsoft.AspNetCore.Builder;
using Serilog;
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
            var environmentName = env.EnvironmentName ?? "Development";

            // Bind Elastic logging options from configuration
            var elasticOptions = ElasticLoggingOptionsResolver.Resolve(configuration);

            var elasticUri = elasticOptions.NodeUris;
            var username = elasticOptions.Username;
            var password = elasticOptions.Password;
            var serviceName = string.IsNullOrWhiteSpace(elasticOptions.ServiceName) ? applicationName : elasticOptions.ServiceName.Trim().ToLowerInvariant();
            var elasticIndexFormat = $"{elasticOptions.IndexPrefix}-{serviceName}-{{0:yyyy.MM.dd}}";
            var elasticEndpointIsValid = TryGetPrimaryEndpoint(elasticUri, out var elasticEndpoint);

            // Create logger configuration
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new EcsLogEventEnricher())
                .Enrich.WithProperty("service.name", serviceName)
                .Enrich.WithProperty("service.environment", environmentName)
                .Enrich.WithProperty("host.name", Environment.MachineName)
                .WriteTo.Async(a =>
                {
                    a.Console(outputTemplate: OutputTemplate);
                    a.File(
                        path: "logs/log-.txt",
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 10_000_000,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 7,
                        shared: true,
                        outputTemplate: OutputTemplate
                    );
                }, bufferSize: 5000)
                .WriteTo.Async(a =>
                {
                    if (elasticOptions.Enabled && elasticEndpointIsValid && elasticEndpoint != null)
                    {
                        a.OpenSearch(new OpenSearchSinkOptions(elasticEndpoint)
                        {
                            IndexFormat = elasticIndexFormat,
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
