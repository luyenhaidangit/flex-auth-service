using Serilog.Core;
using Serilog.Events;

namespace Flex.Infrastructures.Logging
{
    internal sealed class EcsLogEventEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "log.level",
                logEvent.Level.ToString().ToLowerInvariant()));

            if (logEvent.Exception == null)
            {
                return;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "error.type",
                logEvent.Exception.GetType().Name));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "error.message",
                logEvent.Exception.Message));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "error.stack_trace",
                logEvent.Exception.ToString()));
        }
    }
}
