namespace RDCS.EmployeeAgent.Runtime.Screenshot.Models;

public class ScreenshotMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
