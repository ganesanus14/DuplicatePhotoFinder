using DuplicateEngine.Models;

namespace DuplicatePhotoFinder.ViewModels;

public class PhotoFileViewModel : ViewModelBase
{
    private readonly PhotoFile _model;
    private bool _isSelected;

    public PhotoFileViewModel(PhotoFile model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public string FilePath => _model.FilePath;
    public string FileName => _model.FileName;
    public long FileSize => _model.FileSize;
    public DateTime LastModified => _model.LastModified;
    public string Resolution => _model.Resolution;
    public double BlurScore => _model.BlurScore;

    /// <summary>Human-readable blur label for the UI.</summary>
    public string BlurLabel => _model.BlurScore switch
    {
        < 0 => "N/A",
        < 50 => $"Very Blurry ({_model.BlurScore:F0})",
        < 200 => $"Blurry ({_model.BlurScore:F0})",
        _ => $"Sharp ({_model.BlurScore:F0})"
    };

    /// <summary>True when below the blur threshold (set during construction).</summary>
    public bool IsBlurry => _model.BlurScore >= 0 && _model.BlurScore < 200;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
