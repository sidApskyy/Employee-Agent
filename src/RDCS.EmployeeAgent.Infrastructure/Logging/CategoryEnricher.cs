using RDCS.EmployeeAgent.Core.Enums;
using Serilog.Core;
using Serilog.Events;

namespace RDCS.EmployeeAgent.Infrastructure.Logging;

public class CategoryEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue) &&
            sourceContextValue is ScalarValue scalarValue &&
            scalarValue.Value is string sourceContext)
        {
            var category = DetermineCategory(sourceContext);
            var categoryProperty = propertyFactory.CreateProperty("Category", category.ToString());
            logEvent.AddPropertyIfAbsent(categoryProperty);
        }
    }

    private static LogCategory DetermineCategory(string sourceContext)
    {
        if (sourceContext.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
            return LogCategory.Authentication;
        if (sourceContext.Contains("Heartbeat", StringComparison.OrdinalIgnoreCase))
            return LogCategory.Heartbeat;
        if (sourceContext.Contains("Configuration", StringComparison.OrdinalIgnoreCase))
            return LogCategory.Configuration;
        if (sourceContext.Contains("DeviceRegistration", StringComparison.OrdinalIgnoreCase))
            return LogCategory.DeviceRegistration;
        if (sourceContext.Contains("Network", StringComparison.OrdinalIgnoreCase) ||
            sourceContext.Contains("Api", StringComparison.OrdinalIgnoreCase))
            return LogCategory.Network;
        
        return LogCategory.Application;
    }
}
