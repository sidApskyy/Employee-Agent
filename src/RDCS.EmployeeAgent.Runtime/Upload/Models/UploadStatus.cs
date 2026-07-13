namespace RDCS.EmployeeAgent.Runtime.Upload.Models;

public enum UploadStatus
{
    Pending,
    Preparing,
    Uploading,
    Uploaded,
    Completed,
    Failed,
    Retrying,
    Cancelled,
    DeadLetter
}
