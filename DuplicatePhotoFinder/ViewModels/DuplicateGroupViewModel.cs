using DuplicateEngine.Models;
using System.Collections.ObjectModel;

namespace DuplicatePhotoFinder.ViewModels;

public class DuplicateGroupViewModel : ViewModelBase
{
    public int GroupId { get; }
    public double Similarity { get; }
    public int Count => Photos.Count;

    public ObservableCollection<PhotoFileViewModel> Photos { get; }

    public DuplicateGroupViewModel(DuplicateGroup group)
    {
        GroupId = group.GroupId;
        Similarity = group.Similarity;
        Photos = new ObservableCollection<PhotoFileViewModel>(
            group.Photos.Select(p => new PhotoFileViewModel(p)));
    }
}
