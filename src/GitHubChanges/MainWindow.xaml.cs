using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubChanges
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<MainViewModel>();
            Title = ViewModel.WindowTitle;
            
            Loaded += (_, _) => ViewModel.IsActive = true;
            Unloaded += (_, _) => ViewModel.IsActive = false;
        }

        public MainViewModel ViewModel => DataContext as MainViewModel;
    }
}
