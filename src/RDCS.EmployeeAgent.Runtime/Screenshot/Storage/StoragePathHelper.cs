using RDCS.EmployeeAgent.Runtime.Storage;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Storage;

public class StoragePathHelper
{
    private readonly StoragePathProvider _pathProvider;

    public StoragePathHelper(StoragePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public string GetStoragePath(string employeeId, DateTime captureTimeUtc)
    {
        return _pathProvider.GetEmployeeScreenshotFolder(employeeId, captureTimeUtc);
    }

    public string GenerateFileName(DateTime captureTimeUtc, string format)
    {
        var timestamp = ((DateTimeOffset)captureTimeUtc).ToUnixTimeSeconds();
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var extension = format.ToLowerInvariant();

        return $"{timestamp}_{correlationId}.{extension}";
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public string GetBasePath()
    {
        return _pathProvider.GetScreenshotFolder();
    }
}
