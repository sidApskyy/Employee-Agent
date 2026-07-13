using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Storage.Providers;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageProviderFactory
{
    private readonly IAgentLogger _logger;

    public StorageProviderFactory(IAgentLogger logger)
    {
        _logger = logger;
    }

    public IStorageProvider CreateProvider(string providerType, Dictionary<string, string> configuration)
    {
        return providerType.ToLowerInvariant() switch
        {
            "amazons3" => CreateAmazonS3Provider(configuration),
            "cloudflarer2" => throw new NotImplementedException("Cloudflare R2 provider not yet implemented"),
            "azure" => throw new NotImplementedException("Azure Storage provider not yet implemented"),
            _ => throw new ArgumentException($"Unknown storage provider: {providerType}")
        };
    }

    private IStorageProvider CreateAmazonS3Provider(Dictionary<string, string> configuration)
    {
        var bucketName = configuration.GetValueOrDefault("BucketName", string.Empty);
        var accessKey = configuration.GetValueOrDefault("AccessKey", string.Empty);
        var secretKey = configuration.GetValueOrDefault("SecretKey", string.Empty);
        var region = configuration.GetValueOrDefault("Region", "us-east-1");

        _logger.LogInformation(LogCategory.Application, "Creating Amazon S3 storage provider for bucket: {Bucket}", bucketName);

        return new AmazonS3StorageProvider(bucketName, accessKey, secretKey, region, _logger);
    }
}
