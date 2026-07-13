namespace RDCS.EmployeeAgent.Runtime.Storage;

public interface IStorageProvider
{
    string ProviderName { get; }
    Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken = default);
    Task<StorageResponse> DownloadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<List<string>> ListAsync(string prefix, CancellationToken cancellationToken = default);
    Task<StorageResponse> GetMetadataAsync(string key, CancellationToken cancellationToken = default);
}
