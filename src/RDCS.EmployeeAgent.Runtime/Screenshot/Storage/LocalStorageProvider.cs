using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Storage;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Storage;

public class LocalStorageProvider : IStorageProvider
{
    private readonly IAgentLogger _logger;
    private readonly StoragePathHelper _pathHelper;

    public LocalStorageProvider(IAgentLogger logger, StoragePathHelper pathHelper)
    {
        _logger = logger;
        _pathHelper = pathHelper;
    }

    public string ProviderName => "Local";

    public async Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var fullPath = Path.Combine(request.BucketName ?? _pathHelper.GetBasePath(), request.Key);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                _pathHelper.EnsureDirectoryExists(directory);
            }

            using var fileStream = File.Create(fullPath);
            await request.Content.CopyToAsync(fileStream, cancellationToken);

            var fileInfo = new FileInfo(fullPath);

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"File uploaded to local storage: {request.Key} ({fileInfo.Length} bytes) in {stopwatch.ElapsedMilliseconds}ms");

            return new StorageResponse
            {
                Success = true,
                Key = request.Key,
                Url = fullPath,
                SizeBytes = fileInfo.Length,
                UploadedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to upload file to local storage: {request.Key}", ex);
            throw;
        }
    }

    public async Task<StorageResponse> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var fullPath = Path.Combine(_pathHelper.GetBasePath(), key);

            if (!File.Exists(fullPath))
            {
                return new StorageResponse
                {
                    Success = false,
                    Key = key,
                    Error = "File not found"
                };
            }

            var fileStream = File.OpenRead(fullPath);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var fileInfo = new FileInfo(fullPath);

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"File downloaded from local storage: {key} in {stopwatch.ElapsedMilliseconds}ms");

            return new StorageResponse
            {
                Success = true,
                Key = key,
                Url = fullPath,
                SizeBytes = fileInfo.Length,
                Content = memoryStream
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to download file from local storage: {key}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var fullPath = Path.Combine(_pathHelper.GetBasePath(), key);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation(LogCategory.Application, $"File deleted from local storage: {key}");
            }

            stopwatch.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to delete file from local storage: {key}", ex);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_pathHelper.GetBasePath(), key);
            return File.Exists(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to check file existence: {key}", ex);
            return false;
        }
    }

    public async Task<List<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_pathHelper.GetBasePath(), prefix);

            if (!Directory.Exists(fullPath))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
            var basePath = _pathHelper.GetBasePath();

            var keys = files
                .Select(f => f.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/"))
                .ToList();

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to list files in local storage: {prefix}", ex);
            return new List<string>();
        }
    }

    public async Task<StorageResponse> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_pathHelper.GetBasePath(), key);

            if (!File.Exists(fullPath))
            {
                return new StorageResponse
                {
                    Success = false,
                    Key = key,
                    Error = "File not found"
                };
            }

            var fileInfo = new FileInfo(fullPath);

            return new StorageResponse
            {
                Success = true,
                Key = key,
                Url = fullPath,
                SizeBytes = fileInfo.Length,
                UploadedAtUtc = fileInfo.CreationTimeUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to get file metadata: {key}", ex);
            throw;
        }
    }
}
