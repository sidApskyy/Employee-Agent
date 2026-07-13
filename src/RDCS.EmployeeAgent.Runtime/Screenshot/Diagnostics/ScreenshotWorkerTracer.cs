namespace RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;

public static class ScreenshotWorkerTracer
{
    private static readonly object LockObject = new();

    public static void Trace(string message)
    {
        try
        {
            var traceFolder = Path.Combine("C:\\RDCS Agent", "Diagnostics");
            Directory.CreateDirectory(traceFolder);
            var traceFile = Path.Combine(traceFolder, "screenshot-worker-trace.txt");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] {message}{Environment.NewLine}";
            lock (LockObject)
            {
                File.AppendAllText(traceFile, line);
            }
        }
        catch
        {
            // Best effort tracing - do not throw
        }
    }
}
