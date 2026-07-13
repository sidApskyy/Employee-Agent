namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageException : Exception
{
    public string ProviderName { get; }
    public string? Key { get; }

    public StorageException(string providerName, string message) 
        : base(message)
    {
        ProviderName = providerName;
    }

    public StorageException(string providerName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ProviderName = providerName;
    }

    public StorageException(string providerName, string key, string message) 
        : base(message)
    {
        ProviderName = providerName;
        Key = key;
    }
}
