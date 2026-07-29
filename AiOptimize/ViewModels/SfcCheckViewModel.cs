using AiOptimize.Services;

namespace AiOptimize.ViewModels;

public sealed class SfcCheckViewModel : ViewModelBase
{
    private string _statusText = "正在检查并修复系统文件，可能需要 5-15 分钟，请不要关闭本窗口…";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isRunning = true;
    public bool IsRunning { get => _isRunning; set => SetProperty(ref _isRunning, value); }

    private string _resultText = "";
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    private bool _isHealthy;
    public bool IsHealthy { get => _isHealthy; set => SetProperty(ref _isHealthy, value); }

    public SfcCheckViewModel()
    {
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        var result = await SfcRunner.RunAsync();
        IsRunning = false;
        StatusText = "";
        IsHealthy = result.IsHealthy;
        ResultText = (result.IsHealthy ? "✔ " : "⚠ ") + result.Message;
    }
}
