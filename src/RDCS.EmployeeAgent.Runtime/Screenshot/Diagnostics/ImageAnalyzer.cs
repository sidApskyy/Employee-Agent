using System.Drawing;
using System.Drawing.Imaging;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;

public class ImageAnalysisResult
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double AverageR { get; set; }
    public double AverageG { get; set; }
    public double AverageB { get; set; }
    public Color DominantColor { get; set; }
    public double DominantColorPercentage { get; set; }
    public bool IsSuspicious { get; set; }
    public string SuspiciousReason { get; set; } = string.Empty;
}

public static class ImageAnalyzer
{
    private const int ColorTolerance = 15; // RGB tolerance for "similar" colors
    private const double SuspiciousThreshold = 0.80; // 80% threshold

    public static ImageAnalysisResult Analyze(Bitmap bitmap)
    {
        var result = new ImageAnalysisResult
        {
            Width = bitmap.Width,
            Height = bitmap.Height
        };

        var totalPixels = bitmap.Width * bitmap.Height;
        var colorCounts = new Dictionary<Color, int>();
        var totalR = 0;
        var totalG = 0;
        var totalB = 0;

        // Lock bits for fast pixel access
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            var ptr = bmpData.Scan0;
            var bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            var rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            var stride = bmpData.Stride;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var index = y * stride + x * 3;
                    var b = rgbValues[index];
                    var g = rgbValues[index + 1];
                    var r = rgbValues[index + 2];

                    totalR += r;
                    totalG += g;
                    totalB += b;

                    // Quantize color to reduce dictionary size
                    var quantizedR = (r / ColorTolerance) * ColorTolerance;
                    var quantizedG = (g / ColorTolerance) * ColorTolerance;
                    var quantizedB = (b / ColorTolerance) * ColorTolerance;
                    var color = Color.FromArgb(quantizedR, quantizedG, quantizedB);

                    if (colorCounts.ContainsKey(color))
                        colorCounts[color]++;
                    else
                        colorCounts[color] = 1;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        result.AverageR = (double)totalR / totalPixels;
        result.AverageG = (double)totalG / totalPixels;
        result.AverageB = (double)totalB / totalPixels;

        // Find dominant color
        var dominantColor = colorCounts.OrderByDescending(kv => kv.Value).First();
        result.DominantColor = dominantColor.Key;
        result.DominantColorPercentage = (double)dominantColor.Value / totalPixels;

        // Check for suspicious patterns
        result.IsSuspicious = result.DominantColorPercentage >= SuspiciousThreshold;
        
        if (result.IsSuspicious)
        {
            var avg = result.DominantColor;
            if (IsBlue(avg))
                result.SuspiciousReason = $"Blue dominant ({result.DominantColorPercentage:P2})";
            else if (IsBlack(avg))
                result.SuspiciousReason = $"Black dominant ({result.DominantColorPercentage:P2})";
            else if (IsGrey(avg))
                result.SuspiciousReason = $"Grey dominant ({result.DominantColorPercentage:P2})";
            else
                result.SuspiciousReason = $"Solid color dominant ({result.DominantColorPercentage:P2})";
        }

        return result;
    }

    private static bool IsBlue(Color color)
    {
        return color.B > color.R + 30 && color.B > color.G + 30;
    }

    private static bool IsBlack(Color color)
    {
        return color.R < 30 && color.G < 30 && color.B < 30;
    }

    private static bool IsGrey(Color color)
    {
        var diff = Math.Abs(color.R - color.G) + Math.Abs(color.G - color.B) + Math.Abs(color.R - color.B);
        return diff < 30 && color.R > 30 && color.R < 200;
    }
}
