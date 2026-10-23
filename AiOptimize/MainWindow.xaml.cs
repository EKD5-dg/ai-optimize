using System.Reflection;
using System.Windows;
using System.Windows.Threading;
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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // 纯展示窗口：拦掉搜狗等 IME 注入的 64Hz UI 定时器，避免空转耗 CPU
        ImeTimerGuard.Install();
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
        ImeTimerGuard.Suspend(); // 该窗口有搜索框，需要保留中文输入
        try
        {
            var window = new StartupManagerWindow { Owner = this };
            window.ShowDialog();
            _viewModel.RefreshStartupCount();
        }
        finally
        {
            ImeTimerGuard.Resume();
        }
    }

    private void OnBlueScreenClick(object sender, RoutedEventArgs e)
    {
        var window = new BlueScreenWindow { Owner = this };
        window.ShowDialog();
    }

    private DispatcherTimer? _memoryDetailTimer;
    private DispatcherTimer? _cpuDetailTimer;

    /// <summary>内存明细展开时立即刷新一次，并启动每分钟续刷；只在展开期间刷新，收起即停。</summary>
    private void OnMemoryDetailChecked(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.RefreshMemoryDetailsAsync();
        _memoryDetailTimer ??= CreateRefreshTimer(TimeSpan.FromSeconds(60), _viewModel.RefreshMemoryDetailsAsync);
        if (WindowState != WindowState.Minimized) _memoryDetailTimer.Start();
    }

    private void OnMemoryDetailUnchecked(object sender, RoutedEventArgs e) => _memoryDetailTimer?.Stop();

    /// <summary>CPU 明细展开时立即刷新一次，展开期间每 5 秒续刷；收起即停。</summary>
    private void OnCpuDetailChecked(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.RefreshCpuDetailsAsync();
        _cpuDetailTimer ??= CreateRefreshTimer(TimeSpan.FromSeconds(5), _viewModel.RefreshCpuDetailsAsync);
        if (WindowState != WindowState.Minimized) _cpuDetailTimer.Start();
    }

    private void OnCpuDetailUnchecked(object sender, RoutedEventArgs e) => _cpuDetailTimer?.Stop();

    /// <summary>定时刷新：窗口不可见或最小化时跳过，避免在后台白跑进程枚举。</summary>
    private DispatcherTimer CreateRefreshTimer(TimeSpan interval, Func<Task> refresh)
    {
        var timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (_, _) =>
        {
            if (!IsVisible || WindowState == WindowState.Minimized) return;
            _ = refresh();
        };
        return timer;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized) PauseLiveUpdates();
        else ResumeLiveUpdates();
    }

    /// <summary>最小化后暂停每秒监控与明细刷新，恢复窗口时立即补一次数据。</summary>
    private void PauseLiveUpdates()
    {
        _viewModel.PauseMonitoring();
        _cpuDetailTimer?.Stop();
        _memoryDetailTimer?.Stop();
    }

    private void ResumeLiveUpdates()
    {
        _viewModel.ResumeMonitoring();
        if (CpuDetailToggle.IsChecked == true)
        {
            _cpuDetailTimer?.Start();
            _ = _viewModel.RefreshCpuDetailsAsync();
        }
        if (MemoryDetailToggle.IsChecked == true)
        {
            _memoryDetailTimer?.Start();
            _ = _viewModel.RefreshMemoryDetailsAsync();
        }
    }

    /// <summary>明细列表「结束」：确认后结束该行全部同名进程，完成即刷新列表。</summary>
    private async void OnEndCpuProcessClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not AiOptimize.Models.CpuProcessItem item) return;
        var confirm = MessageBox.Show(this,
            $"确定结束「{item.Name}」的 {item.Pids.Count} 个进程吗？\n未保存的数据可能会丢失。",
            "结束进程", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.OK) return;

        int ended = await _viewModel.EndCpuProcessesAsync(item.Pids);
        _viewModel.ResultText = ended == item.Pids.Count
            ? $"已结束 {item.Name}"
            : $"已结束 {ended}/{item.Pids.Count} 个进程（其余可能已退出或无权限）";
        await _viewModel.RefreshCpuDetailsAsync();
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
        _memoryDetailTimer?.Stop();
        _cpuDetailTimer?.Stop();
        _viewModel.Shutdown();
        base.OnClosed(e);
    }
}
