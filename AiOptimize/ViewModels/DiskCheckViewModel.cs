using System.Windows;
using AiOptimize.Services;

namespace AiOptimize.ViewModels;

public sealed class DiskCheckViewModel : ViewModelBase
{
    private string _statusText = "正在检查系统盘（C:），大约需要 1-3 分钟，请稍候…";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isRunning = true;
    public bool IsRunning { get => _isRunning; set => SetProperty(ref _isRunning, value); }

    private string _resultText = "";
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    private bool _isHealthy;
    public bool IsHealthy { get => _isHealthy; set => SetProperty(ref _isHealthy, value); }

    // 已排程修复：磁盘仍不健康，但不需要再显示修复按钮，结果文字用中性色
    private bool _repairScheduled;
    public bool RepairScheduled { get => _repairScheduled; set => SetProperty(ref _repairScheduled, value); }

    private bool _showRepairButton;
    public bool ShowRepairButton { get => _showRepairButton; set => SetProperty(ref _showRepairButton, value); }

    public RelayCommand ScheduleRepairCommand { get; }

    private readonly CancellationTokenSource _cts = new();

    public DiskCheckViewModel()
    {
        ScheduleRepairCommand = new RelayCommand(async _ => await ScheduleRepairAsync());
        _ = RunAsync();
    }

    /// <summary>窗口关闭时取消正在进行的检查（杀掉 chkdsk 进程）。</summary>
    public void Cancel() => _cts.Cancel();

    private async Task RunAsync()
    {
        try
        {
            var result = await DiskCheckRunner.RunScanAsync(_cts.Token);
            IsHealthy = result.IsHealthy;
            ResultText = result.IsHealthy ? "✔ " + result.Message : "⚠ " + result.Message;
            ShowRepairButton = !result.IsHealthy;
        }
        catch (Exception ex)
        {
            ResultText = $"磁盘检查未能完成：{ex.Message}";
        }
        finally
        {
            IsRunning = false;
            StatusText = "";
        }
    }

    private async Task ScheduleRepairAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        StatusText = "正在安排修复…";
        try
        {
            var result = await DiskCheckRunner.ScheduleRepairAsync();
            if (result.IsHealthy)
            {
                ShowRepairButton = false;
                RepairScheduled = true;
                ResultText = "✔ " + result.Message;
            }
            else
            {
                MessageBox.Show(result.Message, "磁盘检查", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            IsRunning = false;
            StatusText = "";
        }
    }
}
