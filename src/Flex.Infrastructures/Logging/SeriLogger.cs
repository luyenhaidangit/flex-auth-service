using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
            var elasticIndexFormat = $"{elasticOptions.IndexPrefix}-{applicationName}-{environmentName}-{DateTime.UtcNow:yyyy.MM.dd}";

            // Create logger configuration
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Environment", environmentName)
                .Enrich.WithProperty("Application", applicationName)
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
                    if (elasticOptions.Enabled && !string.IsNullOrWhiteSpace(elasticUri))
                    {
                        a.OpenSearch(new OpenSearchSinkOptions(new Uri(elasticUri))
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

            if (elasticOptions.Enabled && !string.IsNullOrWhiteSpace(elasticUri))
            {
                Log.Information(
                    "Elasticsearch logging sink initialized. Endpoint={Endpoint}, IndexFormat={IndexFormat}",
                    elasticUri,
                    elasticIndexFormat);
            }
            else
            {
                Log.Information("Elasticsearch logging sink is disabled");
            }

            // Use Serilog
            host.UseSerilog();
        }
    }
}
