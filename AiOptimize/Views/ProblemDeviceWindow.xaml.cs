using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class ProblemDeviceWindow : Window
{
    public ProblemDeviceWindow()
    {
        InitializeComponent();
        DataContext = new ProblemDeviceViewModel();
    }
}
