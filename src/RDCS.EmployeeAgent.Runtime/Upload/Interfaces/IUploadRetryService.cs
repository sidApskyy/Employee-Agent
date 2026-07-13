using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IUploadRetryService
{
    Task<bool> ShouldRetryAsync(UploadJob job, CancellationToken cancellationToken = default);
    Task ScheduleRetryAsync(UploadJob job, string errorMessage, CancellationToken cancellationToken = default);
    Task ProcessDueRetriesAsync(CancellationToken cancellationToken = default);
    TimeSpan CalculateDelay(int retryCount, int baseDelayMinutes);
}
