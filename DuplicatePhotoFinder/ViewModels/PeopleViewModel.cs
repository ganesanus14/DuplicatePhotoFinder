using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using DuplicateEngine.Face;

namespace DuplicatePhotoFinder.ViewModels
{
    public class PeopleViewModel : ViewModelBase
    {
        public ObservableCollection<FaceGroup> FaceGroups { get; } = new();

        public ICommand RefreshCommand { get; }

        private readonly string _folder;

        //public PeopleViewModel(string folder)
        //{
        //    _folder = folder;
        //    RefreshCommand = new RelayCommand(_ => LoadFaces());
        //    LoadFaces();
        //}

        public PeopleViewModel(string? folder)
        {
            _folder = folder ?? "";
            RefreshCommand = new RelayCommand(_ => LoadFaces());

            if (!string.IsNullOrEmpty(_folder))
                LoadFaces();
        }

        private void LoadFaces()
        {
            FaceGroups.Clear();

            var engine = new FaceEngine();
            var grouper = new FaceGrouper();

            var files = Directory.GetFiles(_folder, "*.*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png"))
                                 .ToList();

            var allFaces = new List<FaceInfo>();
            foreach (var file in files)
                allFaces.AddRange(engine.DetectFaces(file));

            var groups = grouper.GroupFaces(allFaces);

            foreach (var g in groups)
                FaceGroups.Add(g);
        }
    }

}
