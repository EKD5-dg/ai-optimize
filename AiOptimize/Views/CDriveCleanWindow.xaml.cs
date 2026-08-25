using System.Windows;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class CDriveCleanWindow : Window
{
    private readonly CDriveCleanViewModel _viewModel;

    public CDriveCleanWindow()
    {
        InitializeComponent();
        _viewModel = new CDriveCleanViewModel();
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Cancel();
    }
}
