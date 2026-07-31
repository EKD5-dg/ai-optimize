using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class SfcCheckWindow : Window
{
    private readonly SfcCheckViewModel _viewModel;

    public SfcCheckWindow()
    {
        InitializeComponent();
        _viewModel = new SfcCheckViewModel();
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Cancel();
    }
}
