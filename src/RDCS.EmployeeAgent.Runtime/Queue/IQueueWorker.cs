namespace RDCS.EmployeeAgent.Runtime.Queue;

public interface IQueueWorker
{
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task StopProcessingAsync(CancellationToken cancellationToken = default);
}
