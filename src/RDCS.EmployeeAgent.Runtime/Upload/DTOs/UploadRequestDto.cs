namespace RDCS.EmployeeAgent.Runtime.Upload.DTOs;

public class UploadRequestDto
{
    public string JobId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public string LocalFilePath { get; set; } = string.Empty;
}
