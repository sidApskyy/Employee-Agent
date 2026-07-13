using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public class ScreenshotService : IScreenshotService
{
    private readonly IAgentLogger _logger;

    public ScreenshotService(IAgentLogger logger)
    {
        _logger = logger;
    }

    public async Task<Stream> CaptureFullDesktopAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var bounds = await GetDesktopBoundsAsync(cancellationToken);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);
            }

            var outputStream = new MemoryStream();
            bitmap.Save(outputStream, ImageFormat.Bmp);
            outputStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"Full desktop captured ({bounds.Width}x{bounds.Height}) in {stopwatch.ElapsedMilliseconds}ms");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Full desktop capture failed", ex);
            throw;
        }
    }

    public async Task<Stream> CaptureMonitorAsync(int monitorId, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var monitors = await GetMonitorInfoAsync(cancellationToken);
            var monitor = monitors.FirstOrDefault(m => m.Id == monitorId);

            if (monitor == null)
            {
                throw new ArgumentException($"Monitor with ID {monitorId} not found");
            }

            var bounds = Screen.AllScreens[monitorId].Bounds;
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);
            }

            var outputStream = new MemoryStream();
            bitmap.Save(outputStream, ImageFormat.Bmp);
            outputStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"Monitor {monitorId} captured ({bounds.Width}x{bounds.Height}) in {stopwatch.ElapsedMilliseconds}ms");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Monitor {monitorId} capture failed", ex);
            throw;
        }
    }

    public async Task<List<MonitorInfo>> GetMonitorInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var screens = Screen.AllScreens;
            var monitorInfos = new List<MonitorInfo>();

            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                using var graphics = Graphics.FromHwnd(IntPtr.Zero);
                var dpiX = (int)(graphics.DpiX * screen.Bounds.Width / screen.WorkingArea.Width);
                var dpiY = (int)(graphics.DpiY * screen.Bounds.Height / screen.WorkingArea.Height);

                monitorInfos.Add(new MonitorInfo
                {
                    Id = i,
                    Name = screen.DeviceName,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    DpiX = dpiX,
                    DpiY = dpiY,
                    IsPrimary = screen.Primary
                });
            }

            _logger.LogInformation(LogCategory.Application, $"Found {monitorInfos.Count} monitors");
            return monitorInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to get monitor info", ex);
            throw;
        }
    }

    public async Task<DesktopBounds> GetDesktopBoundsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var screens = Screen.AllScreens;
            var minX = screens.Min(s => s.Bounds.X);
            var minY = screens.Min(s => s.Bounds.Y);
            var maxX = screens.Max(s => s.Bounds.Right);
            var maxY = screens.Max(s => s.Bounds.Bottom);

            return new DesktopBounds
            {
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to get desktop bounds", ex);
            throw;
        }
    }
}
