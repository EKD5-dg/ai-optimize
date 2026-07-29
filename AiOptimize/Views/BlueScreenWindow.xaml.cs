using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class BlueScreenWindow : Window
{
    public BlueScreenWindow()
    {
        InitializeComponent();
        DataContext = new BlueScreenViewModel();
    }
}
