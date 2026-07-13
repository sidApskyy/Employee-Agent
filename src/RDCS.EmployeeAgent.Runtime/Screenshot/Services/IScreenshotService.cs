namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public interface IScreenshotService
{
    Task<Stream> CaptureFullDesktopAsync(CancellationToken cancellationToken = default);
    Task<Stream> CaptureMonitorAsync(int monitorId, CancellationToken cancellationToken = default);
    Task<List<MonitorInfo>> GetMonitorInfoAsync(CancellationToken cancellationToken = default);
    Task<DesktopBounds> GetDesktopBoundsAsync(CancellationToken cancellationToken = default);
}

public class MonitorInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int DpiX { get; set; }
    public int DpiY { get; set; }
    public bool IsPrimary { get; set; }
}

public class DesktopBounds
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
