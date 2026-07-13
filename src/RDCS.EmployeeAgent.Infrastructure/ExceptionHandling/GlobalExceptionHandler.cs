using RDCS.EmployeeAgent.Core.Interfaces;
using System.Diagnostics;

namespace RDCS.EmployeeAgent.Infrastructure.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IAgentLogger _logger;

    public GlobalExceptionHandler(IAgentLogger logger)
    {
        _logger = logger;
    }

    public Task HandleExceptionAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            _logger.LogError(Core.Enums.LogCategory.Exception, "Unhandled exception occurred", exception);
        }, cancellationToken);
    }

    public static void SetupGlobalHandlers(IAgentLogger logger)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                logger.LogError(Core.Enums.LogCategory.Exception, "AppDomain UnhandledException", ex);
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            logger.LogError(Core.Enums.LogCategory.Exception, "TaskScheduler UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }
}
