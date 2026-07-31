using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class BigFilesWindow : Window
{
    private readonly BigFilesViewModel _viewModel;

    public BigFilesWindow()
    {
        InitializeComponent();
        _viewModel = new BigFilesViewModel();
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Cancel();
    }
}
