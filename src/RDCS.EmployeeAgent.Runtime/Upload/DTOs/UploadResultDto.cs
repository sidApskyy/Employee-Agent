namespace RDCS.EmployeeAgent.Runtime.Upload.DTOs;

public class UploadResultDto
{
    public string UploadId { get; set; } = string.Empty;
    public string S3ObjectKey { get; set; } = string.Empty;
    public string S3Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool ChecksumVerified { get; set; }
}
