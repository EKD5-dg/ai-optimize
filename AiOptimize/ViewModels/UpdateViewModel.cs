using System.Diagnostics;
using System.IO;
using AiOptimize.Services;

namespace AiOptimize.ViewModels;

/// <summary>
/// 更新弹窗 ViewModel：窗口打开时已知有新版本（由启动检查预先查好），
/// 负责下载安装包 → 启动安装。下载失败可重试，取消后回到"下载更新"按钮。
/// </summary>
public sealed class UpdateViewModel : ViewModelBase
{
    private string _newVersionText = "";
    public string NewVersionText { get => _newVersionText; set => SetProperty(ref _newVersionText, value); }

    private string _releaseNotes = "";
    public string ReleaseNotes { get => _releaseNotes; set => SetProperty(ref _releaseNotes, value); }

    private string _statusText = "";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isAvailable = true;
    public bool IsAvailable { get => _isAvailable; set => SetProperty(ref _isAvailable, value); }

    private bool _isDownloading;
    public bool IsDownloading { get => _isDownloading; set => SetProperty(ref _isDownloading, value); }

    private bool _isDownloaded;
    public bool IsDownloaded { get => _isDownloaded; set => SetProperty(ref _isDownloaded, value); }

    private int _progressPercent;
    public int ProgressPercent { get => _progressPercent; set => SetProperty(ref _progressPercent, value); }

    private readonly UpdateInfo _update;
    private readonly CancellationTokenSource _cts = new();
    private string? _downloadedPath;

    public UpdateViewModel(UpdateInfo update)
    {
        _update = update;
        NewVersionText = $"发现新版本 v{update.LatestVersion}";
        ReleaseNotes = string.IsNullOrWhiteSpace(update.ReleaseNotes)
            ? "本次更新说明请查看发布页面。"
            : update.ReleaseNotes;
    }

    public void CancelDownload() => _cts.Cancel();

    /// <summary>下载安装包（UI 线程调用；Progress&lt;T&gt; 会把进度回调到 UI 线程）。</summary>
    public async Task StartDownloadAsync()
    {
        IsAvailable = false;
        IsDownloading = true;
        ProgressPercent = 0;
        StatusText = "正在下载安装包…";

        var progress = new Progress<int>(p =>
        {
            ProgressPercent = p;
            StatusText = $"正在下载安装包… {p}%";
        });

        try
        {
            var fileName = $"AI电脑优化助手安装程序_v{_update.LatestVersion}.exe";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AiOptimize", "update");
            _downloadedPath = await UpdateService.DownloadInstallerAsync(
                _update.DownloadUrl, Path.Combine(dir, fileName), progress, _cts.Token);
            IsDownloaded = true;
            StatusText = "下载完成，点击「立即安装」开始升级。";
        }
        catch (OperationCanceledException)
        {
            IsAvailable = true;
            StatusText = "已取消下载，可以稍后再试。";
        }
        catch (Exception ex)
        {
            IsAvailable = true;
            StatusText = $"下载失败：{ex.Message}，可以点「下载更新」重试。";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    /// <summary>启动安装程序；调用方随后应退出软件，避免正在运行的文件无法被覆盖。</summary>
    public void InstallNow()
    {
        if (_downloadedPath is null) return;
        Process.Start(new ProcessStartInfo(_downloadedPath) { UseShellExecute = true });
    }
}
