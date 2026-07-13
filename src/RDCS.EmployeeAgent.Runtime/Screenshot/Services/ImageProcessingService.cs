using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Screenshot.Models;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly ICompressionProvider _jpegProvider;
    private readonly ICompressionProvider _pngProvider;
    private readonly ICompressionProvider _webpProvider;
    private readonly IAgentLogger _logger;

    public ImageProcessingService(
        JpegCompressionProvider jpegProvider,
        PngCompressionProvider pngProvider,
        WebpCompressionProvider webpProvider,
        IAgentLogger logger)
    {
        _jpegProvider = jpegProvider;
        _pngProvider = pngProvider;
        _webpProvider = webpProvider;
        _logger = logger;
    }

    public async Task<bool> ValidateImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var bitmap = new System.Drawing.Bitmap(imageStream);
            return bitmap.Width > 0 && bitmap.Height > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Image validation failed", ex);
            return false;
        }
    }

    public async Task<Stream> ResizeImageAsync(Stream imageStream, int targetWidth, int targetHeight, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var originalBitmap = new System.Drawing.Bitmap(imageStream);
            var outputStream = new MemoryStream();

            // Calculate aspect ratio
            var scaleX = (double)targetWidth / originalBitmap.Width;
            var scaleY = (double)targetHeight / originalBitmap.Height;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(originalBitmap.Width * scale);
            var newHeight = (int)(originalBitmap.Height * scale);

            using var resizedBitmap = new System.Drawing.Bitmap(originalBitmap, newWidth, newHeight);
            resizedBitmap.Save(outputStream, originalBitmap.RawFormat);
            outputStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"Image resized to {newWidth}x{newHeight} in {stopwatch.ElapsedMilliseconds}ms");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Image resize failed", ex);
            throw;
        }
    }

    public async Task<Stream> CompressImageAsync(Stream imageStream, string format, int quality, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var provider = GetCompressionProvider(format);
            var compressedStream = await provider.CompressAsync(imageStream, quality, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"Image compressed to {format} with quality {quality} in {stopwatch.ElapsedMilliseconds}ms");

            return compressedStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Image compression failed for format {format}", ex);
            throw;
        }
    }

    public async Task<ScreenshotMetadata> GenerateMetadataAsync(Stream imageStream, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var streamLength = imageStream.CanSeek ? imageStream.Length : 0;
            imageStream.Position = 0;
            using var bitmap = new System.Drawing.Bitmap(imageStream);

            return new ScreenshotMetadata
            {
                Width = bitmap.Width,
                Height = bitmap.Height,
                FileSizeBytes = streamLength,
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Metadata generation failed", ex);
            throw;
        }
    }

    public async Task<Stream> ProcessImagePipelineAsync(Stream imageStream, string format, int quality, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Stream currentStream = imageStream;

        try
        {
            // Validate
            if (!await ValidateImageAsync(currentStream, cancellationToken))
            {
                throw new InvalidOperationException("Image validation failed");
            }
            currentStream.Position = 0;

            // Resize (optional)
            if (maxWidth.HasValue && maxHeight.HasValue)
            {
                currentStream = await ResizeImageAsync(currentStream, maxWidth.Value, maxHeight.Value, cancellationToken);
                currentStream.Position = 0;
            }

            // Compress
            currentStream = await CompressImageAsync(currentStream, format, quality, cancellationToken);
            currentStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"Image processing pipeline completed in {stopwatch.ElapsedMilliseconds}ms");

            return currentStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Image processing pipeline failed", ex);
            throw;
        }
    }

    private ICompressionProvider GetCompressionProvider(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => _jpegProvider,
            "png" => _pngProvider,
            "webp" => _webpProvider,
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }
}
