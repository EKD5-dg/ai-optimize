using System.Windows;
using AiOptimize.Services;
using AiOptimize.ViewModels;

namespace AiOptimize.Views;

public partial class UpdateWindow : Window
{
    private readonly UpdateViewModel _viewModel;

    public UpdateWindow(UpdateInfo update)
    {
        InitializeComponent();
        _viewModel = new UpdateViewModel(update);
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.CancelDownload();
    }

    private void OnDownloadClick(object sender, RoutedEventArgs e) => _ = _viewModel.StartDownloadAsync();

    private void OnInstallClick(object sender, RoutedEventArgs e)
    {
        _viewModel.InstallNow();
        Application.Current.Shutdown(); // 退出自身，避免安装程序无法覆盖正在运行的文件
    }

    private void OnLaterClick(object sender, RoutedEventArgs e) => Close();

    private void OnCancelClick(object sender, RoutedEventArgs e) => _viewModel.CancelDownload();
}
