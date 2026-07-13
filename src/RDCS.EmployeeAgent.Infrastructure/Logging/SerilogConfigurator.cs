using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Serilog;
using Serilog.Events;

namespace RDCS.EmployeeAgent.Infrastructure.Logging;

public static class SerilogConfigurator
{
    public static void Configure(string logLevel = "Information")
    {
        var logFolderPath = LogFolderProvider.GetLogFolderPath();
        var minimumLevel = ParseLogLevel(logLevel);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.With(new CategoryEnricher())
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{Category}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logFolderPath, "agent-.log"),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Category}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024)
            .CreateLogger();
    }

    private static LogEventLevel ParseLogLevel(string logLevel)
    {
        return logLevel.ToLowerInvariant() switch
        {
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}

public class SerilogAgentLogger : IAgentLogger
{
    public void LogInformation(LogCategory category, string message, params object[] args)
    {
        Log.ForContext("Category", category).Information(message, args);
    }

    public void LogWarning(LogCategory category, string message, params object[] args)
    {
        Log.ForContext("Category", category).Warning(message, args);
    }

    public void LogError(LogCategory category, string message, Exception? exception = null, params object[] args)
    {
        Log.ForContext("Category", category).Error(exception, message, args);
    }

    public void LogDebug(LogCategory category, string message, params object[] args)
    {
        Log.ForContext("Category", category).Debug(message, args);
    }
}
