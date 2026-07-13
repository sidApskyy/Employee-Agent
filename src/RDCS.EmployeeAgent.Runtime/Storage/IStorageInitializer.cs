namespace RDCS.EmployeeAgent.Runtime.Storage;

public interface IStorageInitializer
{
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateStorageAsync(CancellationToken cancellationToken = default);
}
