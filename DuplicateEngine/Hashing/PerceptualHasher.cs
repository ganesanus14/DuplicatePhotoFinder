using System.Security.Cryptography;
using SkiaSharp;

namespace DuplicateEngine.Hashing;

/// <summary>
/// Provides both a 64-bit perceptual hash (DCT-based pHash)
/// and a SHA-256 content hash for exact duplicate detection.
/// </summary>
public static class PerceptualHasher
{
    private const int HashSize = 8;   // 8×8 = 64-bit hash
    private const int ResizedDimension = 32;  // resize to 32×32 for DCT

    // ── SHA-256 exact hash ────────────────────────────────────────────

    /// <summary>
    /// Returns the hex-encoded SHA-256 hash of the file's raw bytes.
    /// Two files with the same SHA-256 are byte-for-byte identical.
    /// </summary>
    public static string ComputeContentHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }

    // ── Perceptual hash ───────────────────────────────────────────────

    /// <summary>
    /// Computes the 64-bit perceptual hash for the image at <paramref name="filePath"/>.
    /// </summary>
    public static ulong ComputeHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var original = SKBitmap.Decode(stream);

        if (original is null)
            throw new InvalidOperationException($"Cannot decode image: {filePath}");

        using var resized = original.Resize(
            new SKImageInfo(ResizedDimension, ResizedDimension, SKColorType.Gray8),
            SKFilterQuality.Medium);

        if (resized is null)
            throw new InvalidOperationException($"Cannot resize image: {filePath}");

        // Read pixel luminance into a 2-D array
        var pixels = new double[ResizedDimension, ResizedDimension];
        for (int y = 0; y < ResizedDimension; y++)
            for (int x = 0; x < ResizedDimension; x++)
                pixels[y, x] = resized.GetPixel(x, y).Red;

        // Full 2-D DCT
        var dct = ApplyDCT(pixels);

        // Extract the top-left 8×8 low-frequency block
        var block = new double[HashSize * HashSize];
        int idx = 0;
        for (int y = 0; y < HashSize; y++)
            for (int x = 0; x < HashSize; x++)
                block[idx++] = dct[y, x];

        // Mean excluding DC component (index 0)
        double mean = 0;
        for (int i = 1; i < block.Length; i++)
            mean += block[i];
        mean /= (block.Length - 1);

        // Build 64-bit hash
        ulong hash = 0;
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] > mean)
                hash |= 1UL << i;
        }

        return hash;
    }

    /// <summary>
    /// Returns (width, height) without fully decoding the image.
    /// </summary>
    public static (int Width, int Height) GetDimensions(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var codec = SKCodec.Create(stream);
        return codec is null ? (0, 0) : (codec.Info.Width, codec.Info.Height);
    }

    /// <summary>Number of differing bits between two perceptual hashes.</summary>
    public static int HammingDistance(ulong hash1, ulong hash2)
    {
        ulong diff = hash1 ^ hash2;
        int distance = 0;
        while (diff != 0)
        {
            distance++;
            diff &= diff - 1;
        }
        return distance;
    }

    /// <summary>0.0–1.0 similarity score (1.0 = identical).</summary>
    public static double Similarity(ulong hash1, ulong hash2)
        => 1.0 - HammingDistance(hash1, hash2) / 64.0;

    // ── DCT ───────────────────────────────────────────────────────────

    private static double[,] ApplyDCT(double[,] input)
    {
        int n = input.GetLength(0);
        var output = new double[n, n];

        for (int u = 0; u < n; u++)
        {
            double cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
            for (int v = 0; v < n; v++)
            {
                double cv = v == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                double sum = 0;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        sum += input[y, x]
                            * Math.Cos((2.0 * y + 1) * u * Math.PI / (2.0 * n))
                            * Math.Cos((2.0 * x + 1) * v * Math.PI / (2.0 * n));

                output[u, v] = 0.25 * cu * cv * sum;
            }
        }

        return output;
    }
}
