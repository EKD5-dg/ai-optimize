using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class SfcCheckWindow : Window
{
    public SfcCheckWindow()
    {
        InitializeComponent();
        DataContext = new SfcCheckViewModel();
    }
}
