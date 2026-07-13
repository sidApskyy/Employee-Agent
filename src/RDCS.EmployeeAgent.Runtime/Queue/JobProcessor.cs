using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Runtime.Queue;

public class JobProcessor : IJobProcessor
{
    private readonly IAgentLogger _logger;

    public JobProcessor(IAgentLogger logger)
    {
        _logger = logger;
    }

    public Task ProcessJobAsync(IJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Application, $"Processing job {job.JobId} of type {job.JobType}");
        // Default implementation - placeholder for actual job processing logic
        return Task.CompletedTask;
    }

    public Task<bool> CanProcessAsync(IJob job, CancellationToken cancellationToken = default)
    {
        // Default implementation - can process all jobs
        return Task.FromResult(true);
    }
}
