namespace DuplicateEngine.Models;

/// <summary>
/// Controls how aggressively the scanner groups images.
/// </summary>
public enum DetectionMode
{
    /// <summary>SHA-256 byte-for-byte identical files only.</summary>
    ExactOnly = 0,

    /// <summary>pHash with a very tight threshold (Hamming ≤ 2).</summary>
    Strict = 2,

    /// <summary>pHash with a moderate threshold (Hamming ≤ 5).</summary>
    Moderate = 5,

    /// <summary>pHash with a loose threshold (Hamming ≤ 10) — original behaviour.</summary>
    Loose = 10
}
