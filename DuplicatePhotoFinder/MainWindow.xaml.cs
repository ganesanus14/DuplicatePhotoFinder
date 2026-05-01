using System.Windows;
using ModernWpf.Controls;
using DuplicatePhotoFinder.Views;
using DuplicatePhotoFinder.ViewModels;

namespace DuplicatePhotoFinder;

public partial class MainWindow : Window
{
    private MainViewModel _mainViewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        App.EnableMica(this);

        // Create a single shared MainViewModel instance
        _mainViewModel = new MainViewModel();
        DataContext = _mainViewModel;

        // Default to Duplicates tab
        NavView.SelectedItem = NavView.MenuItems[0];
        MainContent.Content = new DuplicatesView
        {
            DataContext = _mainViewModel
        };
    }

    private void NavView_SelectionChanged(NavigationView sender,
                                          NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();

        switch (tag)
        {
            case "Duplicates":
                MainContent.Content = new DuplicatesView
                {
                    DataContext = _mainViewModel
                };
                break;

            case "People":
                MainContent.Content = new PeopleView
                {
                    DataContext = new PeopleViewModel(_mainViewModel.SelectedFolder)
                };
                break;
        }
    }
}
