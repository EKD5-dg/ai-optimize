using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class DiskCheckWindow : Window
{
    private readonly DiskCheckViewModel _viewModel;

    public DiskCheckWindow()
    {
        InitializeComponent();
        _viewModel = new DiskCheckViewModel();
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Cancel();
    }
}
