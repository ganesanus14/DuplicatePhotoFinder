using DuplicateEngine.Analysis;
using DuplicateEngine.Face;
using DuplicateEngine.Hashing;
using DuplicateEngine.Models;

namespace DuplicateEngine.Services;

public class DuplicateScanService
{
    private static readonly string[] SupportedExtensions =
        { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff", ".tif" };

    public DetectionMode Mode { get; set; } = DetectionMode.ExactOnly;
    public int? CustomThreshold { get; set; }

    /// <summary>
    /// Laplacian-variance threshold below which an image is considered blurry.
    /// Default 100.  Set to 0 to disable blur detection.
    /// </summary>
    public double BlurThreshold { get; set; } = 100;

    public event Action<int, int>? ProgressChanged;
    public event Action<string>? StatusChanged;

    // ── Main entry point ──────────────────────────────────────────────

    public async Task<ScanResult> ScanAsync(
        string folderPath,
        bool includeSubfolders,
        CancellationToken cancellationToken = default)
    {
        StatusChanged?.Invoke("Discovering image files…");

        var searchOption = includeSubfolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.EnumerateFiles(folderPath, "*.*", searchOption)
            .Where(f => SupportedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        if (files.Count == 0)
        {
            StatusChanged?.Invoke("No supported image files found.");
            return ScanResult.Empty;
        }

        bool needPHash = Mode != DetectionMode.ExactOnly;
        bool detectBlur = BlurThreshold > 0;

        StatusChanged?.Invoke(
            $"Found {files.Count} images. Hashing{(detectBlur ? " + blur analysis" : "")}…");

        var photos = new List<PhotoFile>();
        int processed = 0;

        await Task.Run(() =>
        {
            Parallel.ForEach(files,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                },
                file =>
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var dims = PerceptualHasher.GetDimensions(file);

                        var photo = new PhotoFile
                        {
                            FilePath = file,
                            FileSize = info.Length,
                            LastModified = info.LastWriteTime,
                            ContentHash = PerceptualHasher.ComputeContentHash(file),
                            PHash = needPHash
                                               ? PerceptualHasher.ComputeHash(file)
                                               : 0,
                            BlurScore = detectBlur
                                               ? BlurDetector.ComputeLaplacianVariance(file)
                                               : -1,
                            Width = dims.Width,
                            Height = dims.Height
                        };

                        lock (photos) photos.Add(photo);
                    }
                    catch { /* skip unreadable files */ }
                    finally
                    {
                        var count = Interlocked.Increment(ref processed);
                        ProgressChanged?.Invoke(count, files.Count);
                    }
                });
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        StatusChanged?.Invoke("Grouping results…");

        // ── Duplicates ────────────────────────────────────────────────
        var duplicates = await Task.Run(
            () => Mode == DetectionMode.ExactOnly
                ? FindExactDuplicates(photos)
                : FindVisualDuplicates(photos),
            cancellationToken);

        // ── Blurry images ─────────────────────────────────────────────
        var blurry = detectBlur
            ? photos
                  .Where(p => p.BlurScore >= 0 && p.BlurScore < BlurThreshold)
                  .OrderBy(p => p.BlurScore)
                  .ToList()
            : new List<PhotoFile>();

        string msg = $"Done — {duplicates.Count} duplicate group(s), "
                   + $"{blurry.Count} blurry image(s).";
        StatusChanged?.Invoke(msg);

        return new ScanResult
        {
            DuplicateGroups = duplicates,
            BlurryPhotos = blurry,
            TotalScanned = photos.Count
        };
    }

    // ── SHA-256 exact grouping ────────────────────────────────────────
    private static List<DuplicateGroup> FindExactDuplicates(List<PhotoFile> photos)
    {
        int groupId = 1;
        return photos
            .GroupBy(p => p.ContentHash)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                GroupId = groupId++,
                Photos = g.ToList(),
                Similarity = 100.0
            })
            .ToList();
    }

    // ── pHash visual grouping ─────────────────────────────────────────
    private List<DuplicateGroup> FindVisualDuplicates(List<PhotoFile> photos)
    {
        int threshold = CustomThreshold ?? (int)Mode;
        var groups = new List<DuplicateGroup>();
        var assigned = new HashSet<int>();
        int groupId = 1;

        for (int i = 0; i < photos.Count; i++)
        {
            if (assigned.Contains(i)) continue;

            var matches = new List<PhotoFile> { photos[i] };
            double worstSim = 1.0;

            for (int j = i + 1; j < photos.Count; j++)
            {
                if (assigned.Contains(j)) continue;

                int distance = PerceptualHasher.HammingDistance(
                    photos[i].PHash, photos[j].PHash);

                if (distance <= threshold)
                {
                    matches.Add(photos[j]);
                    assigned.Add(j);
                    double sim = PerceptualHasher.Similarity(
                        photos[i].PHash, photos[j].PHash);
                    if (sim < worstSim) worstSim = sim;
                }
            }

            if (matches.Count > 1)
            {
                assigned.Add(i);
                groups.Add(new DuplicateGroup
                {
                    GroupId = groupId++,
                    Photos = matches,
                    Similarity = Math.Round(worstSim * 100, 1)
                });
            }
        }

        return groups;
    }

    public List<FaceGroup> ScanFaces(string folder)
    {
        var engine = new FaceEngine();
        var grouper = new FaceGrouper();

        var files = Directory.GetFiles(folder, "*.jpg", SearchOption.AllDirectories)
                             .Concat(Directory.GetFiles(folder, "*.png"))
                             .ToList();

        var allFaces = new List<FaceInfo>();

        foreach (var file in files)
            allFaces.AddRange(engine.DetectFaces(file));

        return grouper.GroupFaces(allFaces);
    }
}
