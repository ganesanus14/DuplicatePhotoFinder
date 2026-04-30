namespace DuplicateEngine.Models;

/// <summary>
/// Holds every result produced by a single scan pass.
/// </summary>
public class ScanResult
{
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
    public List<PhotoFile> BlurryPhotos { get; set; } = new();
    public int TotalScanned { get; set; }

    public static ScanResult Empty => new();
}
