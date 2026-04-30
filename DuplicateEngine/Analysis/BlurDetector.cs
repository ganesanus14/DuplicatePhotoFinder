using SkiaSharp;

namespace DuplicateEngine.Analysis;

/// <summary>
/// Detects image blur using the variance of the Laplacian operator.
/// A low variance means the image lacks sharp edges → blurry.
/// </summary>
public static class BlurDetector
{
    private const int AnalysisMaxDimension = 512;

    /// <summary>
    /// Returns the Laplacian variance for the image.
    ///   • &lt; 50   → very blurry
    ///   • 50–200  → moderately blurry
    ///   • &gt; 200  → acceptably sharp
    /// Returns -1 if the image cannot be decoded.
    /// </summary>
    public static double ComputeLaplacianVariance(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var original = SKBitmap.Decode(stream);
        if (original is null) return -1;

        // Down-scale for speed, keeping aspect ratio
        double scale = Math.Min(1.0,
            (double)AnalysisMaxDimension / Math.Max(original.Width, original.Height));

        int tw = Math.Max(3, (int)(original.Width * scale));
        int th = Math.Max(3, (int)(original.Height * scale));

        using var gray = original.Resize(
            new SKImageInfo(tw, th, SKColorType.Gray8),
            SKFilterQuality.Medium);

        if (gray is null) return -1;

        int w = gray.Width;
        int h = gray.Height;

        // Read luminance
        var px = new double[h, w];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y, x] = gray.GetPixel(x, y).Red;

        // Apply Laplacian kernel  [0,1,0 ; 1,-4,1 ; 0,1,0]
        // and accumulate for one-pass variance (Welford-style sums)
        int count = (h - 2) * (w - 2);
        if (count <= 0) return 0;

        double sum = 0, sumSq = 0;

        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                double lap = -4.0 * px[y, x]
                           + px[y - 1, x]
                           + px[y + 1, x]
                           + px[y, x - 1]
                           + px[y, x + 1];

                sum += lap;
                sumSq += lap * lap;
            }
        }

        double mean = sum / count;
        return (sumSq / count) - (mean * mean);   // variance
    }
}
