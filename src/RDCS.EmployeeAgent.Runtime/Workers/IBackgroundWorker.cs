using RDCS.EmployeeAgent.Core.Interfaces;

namespace RDCS.EmployeeAgent.Runtime.Workers;

public interface IBackgroundWorker
{
    string Name { get; }
    WorkerState State { get; }
    WorkerHealth Health { get; }
    WorkerConfiguration Configuration { get; }
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task<WorkerHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}
