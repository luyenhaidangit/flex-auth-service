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
            var elasticOptions = configuration.GetSection("Logging:Elastic")
                .Get<ElasticLoggingOptions>()
                ?? new ElasticLoggingOptions();

            var elasticUri = elasticOptions.NodeUris;
            var username = elasticOptions.Username;
            var password = elasticOptions.Password;

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
                            IndexFormat = $"{elasticOptions.IndexPrefix}-{applicationName}-{environmentName}-{DateTime.UtcNow:yyyy.MM.dd}",
                            AutoRegisterTemplate = false,
                            ModifyConnectionSettings = c => c.BasicAuthentication(username, password),
                            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
                        });
                    }
                }, bufferSize: 10000);

            // Create logger
            Log.Logger = loggerConfig.CreateLogger();

            // Use Serilog
            host.UseSerilog();
        }
    }
}
