using DuplicateEngine.Models;
using DuplicateEngine.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace DuplicatePhotoFinder.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DuplicateScanService _scanService;
    private CancellationTokenSource? _cts;

    private string? _selectedFolder;
    private bool _isScanning;
    private bool _includeSubfolders = true;
    private int _processedFiles;
    private int _totalFiles;
    private string _statusText = "Ready";
    private string _summaryText = string.Empty;
    private bool _hasResults;
    private bool _hasBlurry;

    private DetectionMode _selectedMode = DetectionMode.ExactOnly;
    private int _customThreshold = 5;
    private bool _isVisualMode;
    private double _blurThreshold = 100;

    public MainViewModel()
    {
        _scanService = new DuplicateScanService();
        DuplicateGroups = new ObservableCollection<DuplicateGroupViewModel>();
        BlurryPhotos = new ObservableCollection<PhotoFileViewModel>();

        AvailableModes = new ObservableCollection<DetectionMode>
        {
            DetectionMode.ExactOnly,
            DetectionMode.Strict,
            DetectionMode.Moderate,
            DetectionMode.Loose
        };

        SelectFolderCommand = new RelayCommand(_ => SelectFolder());
        ScanCommand = new RelayCommand(async _ => await ScanAsync(),
                                                      _ => CanScan);
        CancelScanCommand = new RelayCommand(_ => CancelScan(),
                                                      _ => IsScanning);
        SelectAllDuplicatesCommand = new RelayCommand(_ => AutoSelectDuplicates(),
                                                      _ => HasResults);
        SelectAllBlurryCommand = new RelayCommand(_ => SelectAllBlurry(),
                                                      _ => HasBlurry);
        DeleteSelectedCommand = new RelayCommand(async _ => await DeleteSelectedAsync(),
                                                      _ => HasResults || HasBlurry);
        OpenInExplorerCommand = new RelayCommand(_ => OpenInExplorer(),
                                                      _ => SelectedFolder is not null);

        _scanService.ProgressChanged += (done, total) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessedFiles = done;
                TotalFiles = total;
            });

        _scanService.StatusChanged += msg =>
            Application.Current.Dispatcher.Invoke(() => StatusText = msg);
    }

    // ── Detection mode ────────────────────────────────────────────────

    public ObservableCollection<DetectionMode> AvailableModes { get; }

    public DetectionMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                IsVisualMode = value != DetectionMode.ExactOnly;
                if (value != DetectionMode.ExactOnly)
                    CustomThreshold = (int)value;
            }
        }
    }

    public bool IsVisualMode
    {
        get => _isVisualMode;
        set => SetProperty(ref _isVisualMode, value);
    }

    public int CustomThreshold
    {
        get => _customThreshold;
        set => SetProperty(ref _customThreshold, Math.Clamp(value, 0, 15));
    }

    // ── Blur settings ─────────────────────────────────────────────────

    /// <summary>Laplacian variance below this value = blurry. 0 = disabled.</summary>
    public double BlurThreshold
    {
        get => _blurThreshold;
        set => SetProperty(ref _blurThreshold, Math.Max(0, value));
    }

    public bool HasBlurry
    {
        get => _hasBlurry;
        set => SetProperty(ref _hasBlurry, value);
    }

    public ObservableCollection<PhotoFileViewModel> BlurryPhotos { get; }

    // ── Existing properties ───────────────────────────────────────────

    public string? SelectedFolder
    {
        get => _selectedFolder;
        set { SetProperty(ref _selectedFolder, value); OnPropertyChanged(nameof(CanScan)); }
    }

    public bool IsScanning
    {
        get => _isScanning;
        set { SetProperty(ref _isScanning, value); OnPropertyChanged(nameof(CanScan)); }
    }

    public bool IncludeSubfolders
    {
        get => _includeSubfolders;
        set => SetProperty(ref _includeSubfolders, value);
    }

    public int ProcessedFiles
    {
        get => _processedFiles;
        set => SetProperty(ref _processedFiles, value);
    }

    public int TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

    public bool HasResults
    {
        get => _hasResults;
        set => SetProperty(ref _hasResults, value);
    }

    public bool CanScan => !IsScanning && !string.IsNullOrEmpty(SelectedFolder);

    public ObservableCollection<DuplicateGroupViewModel> DuplicateGroups { get; }

    // ── Commands ──────────────────────────────────────────────────────

    public RelayCommand SelectFolderCommand { get; }
    public RelayCommand ScanCommand { get; }
    public RelayCommand CancelScanCommand { get; }
    public RelayCommand SelectAllDuplicatesCommand { get; }
    public RelayCommand SelectAllBlurryCommand { get; }
    public RelayCommand DeleteSelectedCommand { get; }
    public RelayCommand OpenInExplorerCommand { get; }

    // ── Implementations ───────────────────────────────────────────────

    private void SelectFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder to scan for duplicate photos"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFolder = dialog.FolderName;
            StatusText = $"Selected: {SelectedFolder}";
            DuplicateGroups.Clear();
            BlurryPhotos.Clear();
            HasResults = false;
            HasBlurry = false;
        }
    }

    private async Task ScanAsync()
    {
        if (string.IsNullOrEmpty(SelectedFolder)) return;

        _scanService.Mode = SelectedMode;
        _scanService.CustomThreshold =
            SelectedMode != DetectionMode.ExactOnly ? CustomThreshold : null;
        _scanService.BlurThreshold = BlurThreshold;

        IsScanning = true;
        DuplicateGroups.Clear();
        BlurryPhotos.Clear();
        HasResults = false;
        HasBlurry = false;
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _scanService.ScanAsync(
                SelectedFolder, IncludeSubfolders, _cts.Token);

            // Populate duplicate groups
            foreach (var g in result.DuplicateGroups)
                DuplicateGroups.Add(new DuplicateGroupViewModel(g));

            // Populate blurry images
            foreach (var p in result.BlurryPhotos)
                BlurryPhotos.Add(new PhotoFileViewModel(p));

            HasResults = DuplicateGroups.Count > 0;
            HasBlurry = BlurryPhotos.Count > 0;

            int totalDupPhotos = DuplicateGroups.Sum(g => g.Count);
            SummaryText =
                $"{DuplicateGroups.Count} dup group(s) • {totalDupPhotos} photos • "
              + $"{BlurryPhotos.Count} blurry";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelScan() => _cts?.Cancel();

    private void AutoSelectDuplicates()
    {
        foreach (var group in DuplicateGroups)
        {
            bool first = true;
            foreach (var photo in group.Photos)
            {
                photo.IsSelected = !first;
                first = false;
            }
        }
        StatusText = "Auto-selected duplicates (kept first in each group).";
    }

    private void SelectAllBlurry()
    {
        foreach (var photo in BlurryPhotos)
            photo.IsSelected = true;

        StatusText = $"Selected all {BlurryPhotos.Count} blurry image(s).";
    }

    private async Task DeleteSelectedAsync()
    {
        // Gather from BOTH sections
        var selectedDup = DuplicateGroups
            .SelectMany(g => g.Photos)
            .Where(p => p.IsSelected)
            .ToList();

        var selectedBlur = BlurryPhotos
            .Where(p => p.IsSelected)
            .ToList();

        var allSelected = selectedDup.Concat(selectedBlur)
            .DistinctBy(p => p.FilePath)
            .ToList();

        if (allSelected.Count == 0)
        {
            StatusText = "No photos selected for deletion.";
            return;
        }

        var answer = MessageBox.Show(
            $"Move {allSelected.Count} selected photo(s) to the Recycle Bin?",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes) return;

        var deletedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await Task.Run(() =>
        {
            foreach (var photo in allSelected)
            {
                try
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        photo.FilePath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    deletedPaths.Add(photo.FilePath);
                }
                catch { }
            }
        });

        // Remove from duplicate groups
        foreach (var group in DuplicateGroups.ToList())
        {
            foreach (var p in group.Photos
                         .Where(p => deletedPaths.Contains(p.FilePath)).ToList())
                group.Photos.Remove(p);

            if (group.Photos.Count <= 1)
                DuplicateGroups.Remove(group);
        }

        // Remove from blurry list
        foreach (var p in BlurryPhotos
                     .Where(p => deletedPaths.Contains(p.FilePath)).ToList())
            BlurryPhotos.Remove(p);

        HasResults = DuplicateGroups.Count > 0;
        HasBlurry = BlurryPhotos.Count > 0;

        StatusText = $"Deleted {deletedPaths.Count} photo(s) to Recycle Bin.";
    }

    private void OpenInExplorer()
    {
        if (SelectedFolder is not null)
            Process.Start(new ProcessStartInfo("explorer.exe", SelectedFolder));
    }
}
