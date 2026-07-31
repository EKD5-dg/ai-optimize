using System.Windows;
using AiOptimize.Models;
using AiOptimize.Services;
using AiOptimize.Utils;

namespace AiOptimize.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly SystemMonitorService _monitor = new();
    private readonly TempFileCleaner _tempCleaner = new();
    private readonly DeepCleaner _deepCleaner = new();
    private readonly MemoryOptimizer _memoryOptimizer = new();
    private readonly StartupManager _startupManager = new();
    private readonly BlueScreenAnalyzer _blueScreenAnalyzer = new();

    private double _cpuUsage;
    public double CpuUsage { get => _cpuUsage; set => SetProperty(ref _cpuUsage, value); }

    private double _memoryUsage;
    public double MemoryUsage { get => _memoryUsage; set => SetProperty(ref _memoryUsage, value); }

    private string _memoryText = "";
    public string MemoryText { get => _memoryText; set => SetProperty(ref _memoryText, value); }

    private IReadOnlyList<DiskUsageInfo> _disks = Array.Empty<DiskUsageInfo>();
    public IReadOnlyList<DiskUsageInfo> Disks
    {
        get => _disks;
        set
        {
            // 内容相同则不通知：磁盘列表每秒刷新，引用比较必然不等，
            // 逐元素值比较避免 ItemsControl 每秒全量重建可视化树
            if (_disks.Count == value.Count && _disks.SequenceEqual(value)) return;
            SetProperty(ref _disks, value);
        }
    }

    private bool _isTempSelected = true;
    public bool IsTempSelected { get => _isTempSelected; set => SetProperty(ref _isTempSelected, value); }

    private bool _isDeepSelected = true;
    public bool IsDeepSelected { get => _isDeepSelected; set => SetProperty(ref _isDeepSelected, value); }

    private bool _isMemorySelected = true;
    public bool IsMemorySelected { get => _isMemorySelected; set => SetProperty(ref _isMemorySelected, value); }

    private string _tempScanText = "扫描中…";
    public string TempScanText { get => _tempScanText; set => SetProperty(ref _tempScanText, value); }

    private string _deepScanText = "扫描中…";
    public string DeepScanText { get => _deepScanText; set => SetProperty(ref _deepScanText, value); }

    private IReadOnlyList<ScanItem> _tempDetails = Array.Empty<ScanItem>();
    public IReadOnlyList<ScanItem> TempDetails { get => _tempDetails; set => SetProperty(ref _tempDetails, value); }

    private IReadOnlyList<ScanItem> _deepDetails = Array.Empty<ScanItem>();
    public IReadOnlyList<ScanItem> DeepDetails { get => _deepDetails; set => SetProperty(ref _deepDetails, value); }

    private IReadOnlyList<ScanItem> _memoryDetails = Array.Empty<ScanItem>();
    public IReadOnlyList<ScanItem> MemoryDetails { get => _memoryDetails; set => SetProperty(ref _memoryDetails, value); }

    private string _memoryInfoText = "";
    public string MemoryInfoText { get => _memoryInfoText; set => SetProperty(ref _memoryInfoText, value); }

    private string _startupCountText = "扫描中…";
    public string StartupCountText { get => _startupCountText; set => SetProperty(ref _startupCountText, value); }

    private string _blueScreenText = "扫描中…";
    public string BlueScreenText { get => _blueScreenText; set => SetProperty(ref _blueScreenText, value); }

    private bool _isOptimizing;
    public bool IsOptimizing
    {
        get => _isOptimizing;
        set
        {
            if (SetProperty(ref _isOptimizing, value))
            {
                OnPropertyChanged(nameof(IsNotOptimizing));
                OptimizeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>供复选框 IsEnabled 反向绑定：优化进行中禁止改动选择。</summary>
    public bool IsNotOptimizing => !IsOptimizing;

    private double _progressValue;
    public double ProgressValue { get => _progressValue; set => SetProperty(ref _progressValue, value); }

    private string _progressText = "";
    public string ProgressText { get => _progressText; set => SetProperty(ref _progressText, value); }

    private string _resultText = "";
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    public AsyncRelayCommand OptimizeCommand { get; }

    public MainViewModel()
    {
        OptimizeCommand = new AsyncRelayCommand(
            async _ => await OptimizeAsync(),
            _ => !IsOptimizing && (IsTempSelected || IsDeepSelected || IsMemorySelected),
            ex => ResultText = $"优化过程中出现问题：{ex.Message}");

        _monitor.SnapshotUpdated += OnSnapshot;
        _monitor.Start();
        _ = RefreshScanAsync();
    }

    public void Shutdown() => _monitor.Dispose();

    public void RefreshStartupCount()
    {
        try { StartupCountText = $"{_startupManager.GetItems().Count} 个自启动项"; }
        catch { StartupCountText = "读取失败"; }
    }

    private void OnSnapshot(SystemSnapshot snapshot)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CpuUsage = snapshot.CpuUsage;
            MemoryUsage = snapshot.MemoryUsage;
            MemoryText = $"{ByteFormatter.Format((double)snapshot.MemoryUsedBytes)} / {ByteFormatter.Format((double)snapshot.MemoryTotalBytes)}";
            MemoryInfoText = $"当前使用率 {snapshot.MemoryUsage:0.#}%";
            Disks = snapshot.Disks;
        });
    }

    private async Task RefreshScanAsync()
    {
        TempScanText = "扫描中…";
        DeepScanText = "扫描中…";

        // 每个子扫描独立容错：任一步失败不影响其余步骤，界面也不会卡在"扫描中"
        try
        {
            var tempDetails = await _tempCleaner.ScanDetailsAsync();
            TempDetails = tempDetails;
            TempScanText = $"可清理 {ByteFormatter.Format(tempDetails.Sum(d => d.Bytes))}";
        }
        catch { TempScanText = "读取失败"; }

        try
        {
            var deepDetails = await _deepCleaner.ScanDetailsAsync();
            DeepDetails = deepDetails;
            DeepScanText = $"可清理 {ByteFormatter.Format(deepDetails.Sum(d => d.Bytes))}";
        }
        catch { DeepScanText = "读取失败"; }

        try { MemoryDetails = await _memoryOptimizer.ScanDetailsAsync(); }
        catch { /* 内存明细读取失败时保留旧值 */ }

        try
        {
            var blueScreens = await _blueScreenAnalyzer.GetEventsAsync();
            BlueScreenText = blueScreens.Count > 0 ? $"发现 {blueScreens.Count} 次蓝屏" : "未发现蓝屏记录";
        }
        catch { BlueScreenText = "读取失败"; }

        try { await Task.Run(RefreshStartupCount); }
        catch { /* RefreshStartupCount 内部已兜底 */ }
    }

    private async Task OptimizeAsync()
    {
        IsOptimizing = true;
        ResultText = "";
        ProgressValue = 0;
        var summary = new List<string>();
        var total = new CleanResult();
        // 快照选择状态：优化过程中复选框已禁用，但防御性快照避免中途变化导致进度计算错误
        bool doTemp = IsTempSelected, doDeep = IsDeepSelected, doMemory = IsMemorySelected;
        int steps = (doTemp ? 1 : 0) + (doDeep ? 1 : 0) + (doMemory ? 1 : 0);
        if (steps == 0) { IsOptimizing = false; return; }
        int done = 0;

        try
        {
            if (doTemp)
            {
                ProgressText = "正在清理临时文件…";
                total.Merge(await _tempCleaner.CleanAsync());
                ProgressValue = ++done * 100.0 / steps;
            }

            if (doDeep)
            {
                ProgressText = "正在深度清理垃圾文件…";
                total.Merge(await _deepCleaner.CleanAsync());
                ProgressValue = ++done * 100.0 / steps;
            }

            if (total.BytesFreed > 0)
                summary.Add($"释放磁盘空间 {ByteFormatter.Format(total.BytesFreed)}");

            if (doMemory)
            {
                ProgressText = "正在释放内存…";
                var mem = await _memoryOptimizer.OptimizeAsync();
                ProgressValue = ++done * 100.0 / steps;
                summary.Add($"内存使用率 {mem.BeforeUsage:0.#}% → {mem.AfterUsage:0.#}%");
            }

            if (total.FilesSkipped > 0)
                summary.Add($"跳过 {total.FilesSkipped} 个被占用文件");
            foreach (var note in total.Notes.Distinct())
                summary.Add(note);

            ProgressText = "优化完成";
            ResultText = summary.Count > 0
                ? "✔ " + string.Join("，", summary)
                : "✔ 已完成，没有发现需要清理的内容";
        }
        catch (Exception ex)
        {
            ProgressText = "";
            ResultText = $"优化过程中出现问题：{ex.Message}";
        }
        finally
        {
            IsOptimizing = false;
            _ = RefreshScanAsync();
        }
    }
}
