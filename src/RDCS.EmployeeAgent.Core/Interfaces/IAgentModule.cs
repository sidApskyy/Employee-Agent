using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IAgentModule
{
    string Name { get; }
    ModuleState State { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
