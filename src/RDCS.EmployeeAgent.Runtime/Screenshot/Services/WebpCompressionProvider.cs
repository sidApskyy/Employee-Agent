using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public class WebpCompressionProvider : ICompressionProvider
{
    private readonly IAgentLogger _logger;

    public WebpCompressionProvider(IAgentLogger logger)
    {
        _logger = logger;
    }

    public string ProviderName => "WEBP";
    public string[] SupportedFormats => new[] { "webp" };

    public async Task<Stream> CompressAsync(Stream inputStream, int quality, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Note: WEBP support requires System.Drawing.Common with WEBP codec or external library
            // For now, we'll use JPEG as fallback with a warning
            _logger.LogWarning(LogCategory.Application, "WEBP compression not fully supported, falling back to JPEG");

            using var bitmap = new System.Drawing.Bitmap(inputStream);
            var outputStream = new MemoryStream();

            var jpegEncoder = GetJpegEncoder();
            var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
            encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            bitmap.Save(outputStream, jpegEncoder, encoderParams);
            outputStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"WEBP compression (fallback to JPEG) completed in {stopwatch.ElapsedMilliseconds}ms with quality {quality}");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "WEBP compression failed", ex);
            throw;
        }
    }

    private System.Drawing.Imaging.ImageCodecInfo GetJpegEncoder()
    {
        var codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid)
            ?? throw new InvalidOperationException("JPEG encoder not found");
    }
}
