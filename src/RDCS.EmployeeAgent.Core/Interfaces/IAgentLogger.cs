using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IAgentLogger
{
    void LogInformation(LogCategory category, string message, params object[] args);
    void LogWarning(LogCategory category, string message, params object[] args);
    void LogError(LogCategory category, string message, Exception? exception = null, params object[] args);
    void LogDebug(LogCategory category, string message, params object[] args);
}
