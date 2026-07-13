namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public interface ICompressionProvider
{
    string ProviderName { get; }
    string[] SupportedFormats { get; }
    Task<Stream> CompressAsync(Stream inputStream, int quality, CancellationToken cancellationToken = default);
}
