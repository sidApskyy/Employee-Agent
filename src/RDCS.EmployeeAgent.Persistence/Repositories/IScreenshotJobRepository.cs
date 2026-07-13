using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public interface IScreenshotJobRepository
{
    Task SaveAsync(ScreenshotJob job, CancellationToken cancellationToken = default);
    Task<ScreenshotJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<ScreenshotJob>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string id, string status, string? error, CancellationToken cancellationToken = default);
    Task IncrementRetryAsync(string id, CancellationToken cancellationToken = default);
}
