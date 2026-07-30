using Serilog.Core;
using Serilog.Events;

namespace SIGH.Infrastructure.Logging;

public class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdPropertyName = "CorrelationId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Garante que o CorrelationId esteja presente nos logs estruturados
        if (!logEvent.Properties.ContainsKey(CorrelationIdPropertyName))
        {
            var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, Guid.NewGuid().ToString());
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
