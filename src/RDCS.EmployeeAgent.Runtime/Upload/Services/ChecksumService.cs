using System.Security.Cryptography;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class ChecksumService : IChecksumService
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return await ComputeSha256Async(stream, cancellationToken);
    }

    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool Verify(string filePath, string expectedChecksum)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            var actual = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return string.Equals(actual, expectedChecksum.ToLowerInvariant(), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
