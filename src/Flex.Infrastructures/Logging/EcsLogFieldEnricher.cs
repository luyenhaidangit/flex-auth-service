using Flex.Infrastructures.Observability;
using Serilog.Core;
using Serilog.Events;

namespace Flex.Infrastructures.Logging
{
    public class EcsLogFieldEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                LogFields.LogLevel,
                logEvent.Level.ToString().ToLowerInvariant()));

            if (logEvent.Exception == null)
            {
                return;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                LogFields.ErrorType,
                logEvent.Exception.GetType().Name));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                LogFields.ErrorMessage,
                logEvent.Exception.Message));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                LogFields.ErrorStackTrace,
                logEvent.Exception.ToString()));
        }
    }
}
