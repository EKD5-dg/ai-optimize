using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class BigFilesWindow : Window
{
    public BigFilesWindow()
    {
        InitializeComponent();
        DataContext = new BigFilesViewModel();
    }
}
