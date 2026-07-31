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

    private readonly CancellationTokenSource _cts = new();

    public SfcCheckViewModel()
    {
        _ = RunAsync();
    }

    /// <summary>窗口关闭时取消正在进行的检查（杀掉 sfc 进程）。</summary>
    public void Cancel() => _cts.Cancel();

    private async Task RunAsync()
    {
        try
        {
            var result = await SfcRunner.RunAsync(_cts.Token);
            IsHealthy = result.IsHealthy;
            ResultText = (result.IsHealthy ? "✔ " : "⚠ ") + result.Message;
        }
        catch (Exception ex)
        {
            ResultText = $"系统文件检查未能完成：{ex.Message}";
        }
        finally
        {
            IsRunning = false;
            StatusText = "";
        }
    }
}
