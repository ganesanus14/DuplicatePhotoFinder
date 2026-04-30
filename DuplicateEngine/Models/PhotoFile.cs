namespace DuplicateEngine.Models;

public class PhotoFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }

    /// <summary>Perceptual hash (visual similarity).</summary>
    public ulong PHash { get; set; }

    /// <summary>SHA-256 content hash (exact byte match).</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Laplacian variance — lower value = blurrier.
    /// -1 means the score could not be computed.
    /// </summary>
    public double BlurScore { get; set; } = -1;

    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution => $"{Width} × {Height}";
}
