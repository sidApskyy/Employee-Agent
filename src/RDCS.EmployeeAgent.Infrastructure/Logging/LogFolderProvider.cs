using System.IO;

namespace RDCS.EmployeeAgent.Infrastructure.Logging;

public static class LogFolderProvider
{
    public static string GetLogFolderPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logFolder = Path.Combine(localAppData, "RDCS", "EmployeeAgent", "logs");
        
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }
        
        return logFolder;
    }
}
