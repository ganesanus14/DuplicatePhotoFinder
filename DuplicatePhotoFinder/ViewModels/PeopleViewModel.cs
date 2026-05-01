using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using DuplicateEngine.Face;

namespace DuplicatePhotoFinder.ViewModels
{
    public class PeopleViewModel : ViewModelBase
    {
        private bool _isDetecting;
        private string _statusText = "Ready";

        public ObservableCollection<FaceGroup> FaceGroups { get; } = new();

        public ICommand RefreshCommand { get; }

        private readonly string _folder;

        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public PeopleViewModel(string? folder)
        {
            _folder = folder ?? "";
            RefreshCommand = new RelayCommand(_ => LoadFaces(), _ => !IsDetecting && !string.IsNullOrEmpty(_folder));

            if (!string.IsNullOrEmpty(_folder))
                LoadFaces();
        }

        private void LoadFaces()
        {
            if (string.IsNullOrEmpty(_folder))
            {
                StatusText = "No folder selected. Please select a folder in the Duplicates tab.";
                return;
            }

            IsDetecting = true;
            StatusText = "Detecting faces...";
            FaceGroups.Clear();

            try
            {
                var engine = new FaceEngine();
                var grouper = new FaceGrouper();

                var files = Directory.GetFiles(_folder, "*.*", SearchOption.AllDirectories)
                                     .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                                     .ToList();

                if (files.Count == 0)
                {
                    StatusText = "No images found in the selected folder.";
                    IsDetecting = false;
                    return;
                }

                StatusText = $"Processing {files.Count} image(s)...";

                var allFaces = new List<FaceInfo>();
                foreach (var file in files)
                {
                    try
                    {
                        allFaces.AddRange(engine.DetectFaces(file));
                    }
                    catch
                    {
                        // Skip files that can't be processed
                    }
                }

                if (allFaces.Count == 0)
                {
                    StatusText = "No faces detected in the selected folder.";
                    IsDetecting = false;
                    return;
                }

                var groups = grouper.GroupFaces(allFaces);

                foreach (var g in groups)
                    FaceGroups.Add(g);

                StatusText = $"Found {groups.Count} person group(s) with {allFaces.Count} face(s)";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsDetecting = false;
            }
        }
    }

}
