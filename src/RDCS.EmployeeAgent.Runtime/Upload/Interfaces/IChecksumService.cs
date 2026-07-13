namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IChecksumService
{
    Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default);
    Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken = default);
    bool Verify(string filePath, string expectedChecksum);
}
