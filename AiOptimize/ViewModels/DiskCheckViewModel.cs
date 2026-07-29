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

    private bool _showRepairButton;
    public bool ShowRepairButton { get => _showRepairButton; set => SetProperty(ref _showRepairButton, value); }

    public RelayCommand ScheduleRepairCommand { get; }

    public DiskCheckViewModel()
    {
        ScheduleRepairCommand = new RelayCommand(_ => ScheduleRepair());
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        var result = await DiskCheckRunner.RunScanAsync();
        IsRunning = false;
        StatusText = "";
        IsHealthy = result.IsHealthy;
        ResultText = result.IsHealthy ? "✔ " + result.Message : "⚠ " + result.Message;
        ShowRepairButton = !result.IsHealthy;
    }

    private void ScheduleRepair()
    {
        var result = DiskCheckRunner.ScheduleRepair();
        if (result.IsHealthy)
        {
            ShowRepairButton = false;
            IsHealthy = true;
            ResultText = "✔ " + result.Message;
        }
        else
        {
            MessageBox.Show(result.Message, "磁盘检查", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
