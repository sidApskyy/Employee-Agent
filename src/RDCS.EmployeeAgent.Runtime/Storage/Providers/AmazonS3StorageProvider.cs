using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Runtime.Storage.Providers;

public class AmazonS3StorageProvider : IStorageProvider
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3Client;
    private readonly IAgentLogger _logger;

    public string ProviderName => "AmazonS3";

    public AmazonS3StorageProvider(string bucketName, string accessKey, string secretKey, string region, IAgentLogger logger)
    {
        _bucketName = bucketName;
        _logger = logger;

        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var transferUtility = new TransferUtility(_s3Client);
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _bucketName,
                Key = request.Key,
                InputStream = request.Content,
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.Private
            };

            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, 
                $"File uploaded to S3: {request.Key} (bucket: {_bucketName}) in {stopwatch.ElapsedMilliseconds}ms");

            return new StorageResponse
            {
                Success = true,
                Key = request.Key,
                Url = $"https://{_bucketName}.s3.amazonaws.com/{request.Key}",
                SizeBytes = request.Content.Length,
                UploadedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(LogCategory.Exception, $"Failed to upload file to S3: {request.Key}", ex);
            throw;
        }
    }

    public async Task<StorageResponse> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, 
                $"File downloaded from S3: {key} in {stopwatch.ElapsedMilliseconds}ms");

            return new StorageResponse
            {
                Success = true,
                Key = key,
                Url = $"https://{_bucketName}.s3.amazonaws.com/{key}",
                Content = memoryStream,
                SizeBytes = memoryStream.Length
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new StorageResponse
            {
                Success = false,
                Key = key,
                Error = "File not found"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(LogCategory.Exception, $"Failed to download file from S3: {key}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _s3Client.DeleteObjectAsync(_bucketName, key, cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"File deleted from S3: {key}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(LogCategory.Exception, $"Failed to delete file from S3: {key}", ex);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to check file existence in S3: {key}", ex);
            return false;
        }
    }

    public async Task<List<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

            return response.S3Objects.Select(obj => obj.Key).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to list S3 objects with prefix: {prefix}", ex);
            return new List<string>();
        }
    }

    public async Task<StorageResponse> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, key, cancellationToken);

            return new StorageResponse
            {
                Success = true,
                Key = key,
                Url = $"https://{_bucketName}.s3.amazonaws.com/{key}",
                SizeBytes = metadata.ContentLength,
                UploadedAtUtc = metadata.LastModified
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new StorageResponse
            {
                Success = false,
                Key = key,
                Error = "File not found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to get S3 metadata: {key}", ex);
            throw;
        }
    }
}
