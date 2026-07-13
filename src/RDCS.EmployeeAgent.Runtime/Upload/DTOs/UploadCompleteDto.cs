namespace RDCS.EmployeeAgent.Runtime.Upload.DTOs;

public class UploadCompleteDto
{
    public string JobId { get; set; } = string.Empty;
    public string UploadId { get; set; } = string.Empty;
    public string S3ObjectKey { get; set; } = string.Empty;
}
