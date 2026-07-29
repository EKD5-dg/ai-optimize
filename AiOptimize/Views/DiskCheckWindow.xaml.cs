using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class DiskCheckWindow : Window
{
    public DiskCheckWindow()
    {
        InitializeComponent();
        DataContext = new DiskCheckViewModel();
    }
}
