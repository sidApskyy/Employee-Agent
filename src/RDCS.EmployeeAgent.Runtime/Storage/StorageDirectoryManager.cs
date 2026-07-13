using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Microsoft.Extensions.Options;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageDirectoryManager
{
    private readonly StorageSettings _settings;
    private readonly IAgentLogger _logger;

    public StorageDirectoryManager(
        IOptions<StorageSettings> settings,
        IAgentLogger logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnsureDirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            await CreateDirectoryAsync(path, cancellationToken);
        }
    }

    public async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(path);
            _logger.LogInformation(LogCategory.Application, $"Created directory: {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to create directory: {path}", ex);
            throw;
        }
    }

    public async Task<bool> ValidatePathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            // Prevent path traversal attacks
            if (path.Contains("..") || path.Contains("~"))
            {
                _logger.LogWarning(LogCategory.Application, $"Invalid path detected (traversal attempt): {path}");
                return false;
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                _logger.LogWarning(LogCategory.Application, $"Invalid path characters detected: {path}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Path validation failed for: {path}", ex);
            return false;
        }
    }

    public async Task<long> GetDirectorySizeAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            long totalSize = 0;
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalSize += new FileInfo(file).Length;
            }

            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to calculate directory size for: {path}", ex);
            return 0;
        }
    }

    public async Task<int> GetFileCountAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to count files in: {path}", ex);
            return 0;
        }
    }
}
