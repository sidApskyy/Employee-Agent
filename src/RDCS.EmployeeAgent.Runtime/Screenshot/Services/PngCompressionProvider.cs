using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using System.Drawing.Imaging;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Services;

public class PngCompressionProvider : ICompressionProvider
{
    private readonly IAgentLogger _logger;

    public PngCompressionProvider(IAgentLogger logger)
    {
        _logger = logger;
    }

    public string ProviderName => "PNG";
    public string[] SupportedFormats => new[] { "png" };

    public async Task<Stream> CompressAsync(Stream inputStream, int quality, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var bitmap = new System.Drawing.Bitmap(inputStream);
            var outputStream = new MemoryStream();

            var pngEncoder = GetPngEncoder();
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

            bitmap.Save(outputStream, pngEncoder, encoderParams);
            outputStream.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation(LogCategory.Application, $"PNG compression completed in {stopwatch.ElapsedMilliseconds}ms");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "PNG compression failed", ex);
            throw;
        }
    }

    private ImageCodecInfo GetPngEncoder()
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Png.Guid)
            ?? throw new InvalidOperationException("PNG encoder not found");
    }
}
