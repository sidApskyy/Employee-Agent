using RDCS.EmployeeAgent.Runtime.Screenshot.Models;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public interface IImageProcessingService
{
    Task<bool> ValidateImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<Stream> ResizeImageAsync(Stream imageStream, int targetWidth, int targetHeight, CancellationToken cancellationToken = default);
    Task<Stream> CompressImageAsync(Stream imageStream, string format, int quality, CancellationToken cancellationToken = default);
    Task<ScreenshotMetadata> GenerateMetadataAsync(Stream imageStream, string filePath, CancellationToken cancellationToken = default);
    Task<Stream> ProcessImagePipelineAsync(Stream imageStream, string format, int quality, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default);
}
