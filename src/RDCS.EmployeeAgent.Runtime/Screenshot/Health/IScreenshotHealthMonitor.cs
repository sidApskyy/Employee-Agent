namespace RDCS.EmployeeAgent.Runtime.Screenshot.Health;

public interface IScreenshotHealthMonitor
{
    Task<DateTime?> GetLastCaptureTimeAsync(CancellationToken cancellationToken = default);
    Task<double> GetAverageCaptureDurationAsync(CancellationToken cancellationToken = default);
    Task<double> GetAverageCompressionTimeAsync(CancellationToken cancellationToken = default);
    Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default);
    Task<long> GetStorageUsageAsync(CancellationToken cancellationToken = default);
    Task<int> GetFailureCountAsync(CancellationToken cancellationToken = default);
    Task<double> GetSuccessRateAsync(CancellationToken cancellationToken = default);
}
