using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using System.Drawing.Imaging;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public class JpegCompressionProvider : ICompressionProvider
{
    private readonly IAgentLogger _logger;

    public JpegCompressionProvider(IAgentLogger logger)
    {
        _logger = logger;
    }

    public string ProviderName => "JPEG";
    public string[] SupportedFormats => new[] { "jpg", "jpeg" };

    public async Task<Stream> CompressAsync(Stream inputStream, int quality, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var bitmap = new System.Drawing.Bitmap(inputStream);
            var outputStream = new MemoryStream();

            var jpegEncoder = GetJpegEncoder();
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            bitmap.Save(outputStream, jpegEncoder, encoderParams);
            outputStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"JPEG compression completed in {stopwatch.ElapsedMilliseconds}ms with quality {quality}");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "JPEG compression failed", ex);
            throw;
        }
    }

    private ImageCodecInfo GetJpegEncoder()
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid)
            ?? throw new InvalidOperationException("JPEG encoder not found");
    }
}
