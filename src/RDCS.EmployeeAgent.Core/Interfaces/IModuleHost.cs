namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IModuleHost
{
    Task StartAllModulesAsync(CancellationToken cancellationToken);
    Task StopAllModulesAsync(CancellationToken cancellationToken);
}
