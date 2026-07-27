using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class StartupManagerWindow : Window
{
    public StartupManagerWindow()
    {
        InitializeComponent();
        DataContext = new StartupManagerViewModel();
    }
}
