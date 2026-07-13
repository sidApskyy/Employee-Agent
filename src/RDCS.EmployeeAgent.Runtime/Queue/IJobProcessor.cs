namespace RDCS.EmployeeAgent.Runtime.Queue;

public interface IJobProcessor
{
    Task ProcessJobAsync(IJob job, CancellationToken cancellationToken = default);
    Task<bool> CanProcessAsync(IJob job, CancellationToken cancellationToken = default);
}
