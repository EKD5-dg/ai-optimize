using System.Reflection;
using System.Windows;
using AiOptimize.Services;
using AiOptimize.ViewModels;
using AiOptimize.Views;

namespace AiOptimize;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        // 后台静默检查更新：失败不打扰，发现新版本才弹窗
        await CheckForUpdatesAsync();
    }

    /// <summary>延迟数秒后查询 GitHub Releases；有新版本才弹出更新窗口。</summary>
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2)); // 等主窗口先渲染完，避免启动卡顿
            if (!IsVisible) return;
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
            var update = await UpdateService.CheckLatestAsync(currentVersion);
            if (update is null) return;
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                    new UpdateWindow(update) { Owner = this }.ShowDialog();
            });
        }
        catch
        {
            // 更新检查失败不影响使用
        }
    }

    private void OnManageStartupClick(object sender, RoutedEventArgs e)
    {
        var window = new StartupManagerWindow { Owner = this };
        window.ShowDialog();
        _viewModel.RefreshStartupCount();
    }

    private void OnBlueScreenClick(object sender, RoutedEventArgs e)
    {
        var window = new BlueScreenWindow { Owner = this };
        window.ShowDialog();
    }

    private void OnHelpClick(object sender, RoutedEventArgs e)
    {
        var window = new HelpWindow { Owner = this };
        window.ShowDialog();
    }

    private void OnBigFilesClick(object sender, RoutedEventArgs e)
    {
        var window = new BigFilesWindow { Owner = this };
        window.ShowDialog();
    }

    private void OnCDriveCleanClick(object sender, RoutedEventArgs e)
    {
        var window = new CDriveCleanWindow { Owner = this };
        window.ShowDialog();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Shutdown();
        base.OnClosed(e);
    }
}
