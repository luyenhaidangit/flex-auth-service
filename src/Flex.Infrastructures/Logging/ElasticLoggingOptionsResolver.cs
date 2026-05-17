using Microsoft.Extensions.Configuration;

namespace Flex.Infrastructures.Logging
{
    internal static class ElasticLoggingOptionsResolver
    {
        public static ElasticLoggingOptions Resolve(IConfiguration configuration)
        {
            var loggingElastic = configuration.GetSection("Logging:Elastic")
                .Get<ElasticLoggingOptions>();

            if (!string.IsNullOrWhiteSpace(loggingElastic?.NodeUris))
            {
                return loggingElastic;
            }

            return configuration.GetSection("Elastic")
                .Get<ElasticLoggingOptions>()
                ?? loggingElastic
                ?? new ElasticLoggingOptions();
        }
    }
}
