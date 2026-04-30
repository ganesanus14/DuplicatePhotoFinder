using System.Windows;
using ModernWpf.Controls;
using DuplicatePhotoFinder.Views;
using DuplicatePhotoFinder.ViewModels;

namespace DuplicatePhotoFinder;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        App.EnableMica(this);

        // Default to Duplicates tab
        NavView.SelectedItem = NavView.MenuItems[0];
        MainContent.Content = new DuplicatesView
        {
            DataContext = new MainViewModel()
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
                    DataContext = new MainViewModel()
                };
                break;

            case "People":
                var mainVM = DataContext as MainViewModel;

                MainContent.Content = new PeopleView
                {
                    DataContext = new PeopleViewModel(mainVM?.SelectedFolder)
                };
                break;
        }
    }
}
