namespace RDCS.EmployeeAgent.Runtime.Screenshot.Workers;

public interface IScreenshotWorker
{
    Task<bool> ShouldCaptureAsync(CancellationToken cancellationToken = default);
}
