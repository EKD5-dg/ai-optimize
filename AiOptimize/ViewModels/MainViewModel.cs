using System.IO;
using System.Reflection;
using System.Windows;
using AiOptimize.Models;
using AiOptimize.Services;
using AiOptimize.Utils;

namespace AiOptimize.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    /// <summary>当前版本号（与 csproj 的 &lt;Version&gt; 保持一致），显示在主窗口标题旁。</summary>
    public string VersionText =>
        "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

    private readonly SystemMonitorService _monitor = new();
    private readonly TempFileCleaner _tempCleaner = new();
    private readonly DeepCleaner _deepCleaner = new();
    private readonly MemoryOptimizer _memoryOptimizer = new();
    private readonly CpuUsageScanner _cpuScanner = new();
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

    /// <summary>CPU 降温默认不勾选：会结束/降速其他程序，应由用户主动选择。</summary>
    private bool _isCpuCoolSelected;
    public bool IsCpuCoolSelected { get => _isCpuCoolSelected; set => SetProperty(ref _isCpuCoolSelected, value); }

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

    private IReadOnlyList<CpuProcessItem> _cpuDetails = Array.Empty<CpuProcessItem>();
    public IReadOnlyList<CpuProcessItem> CpuDetails { get => _cpuDetails; set => SetProperty(ref _cpuDetails, value); }

    private string _startupCountText = "扫描中…";
    public string StartupCountText { get => _startupCountText; set => SetProperty(ref _startupCountText, value); }

    private string _blueScreenText = "扫描中…";
    public string BlueScreenText { get => _blueScreenText; set => SetProperty(ref _blueScreenText, value); }

    private string _cDriveSummaryText = "";
    public string CDriveSummaryText { get => _cDriveSummaryText; set => SetProperty(ref _cDriveSummaryText, value); }

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
            _ => !IsOptimizing && (IsTempSelected || IsDeepSelected || IsMemorySelected || IsCpuCoolSelected),
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

    /// <summary>重扫最占内存的程序明细。明细面板展开瞬间调用一次，展开期间由定时器每分钟续刷。</summary>
    public async Task RefreshMemoryDetailsAsync()
    {
        try { MemoryDetails = await _memoryOptimizer.ScanDetailsAsync(); }
        catch { /* 扫描失败时保留旧列表 */ }
    }

    /// <summary>重扫最占 CPU 的程序明细。明细面板展开瞬间调用一次，展开期间由定时器续刷（CPU 波动快，刷新比内存密）。</summary>
    public async Task RefreshCpuDetailsAsync()
    {
        try { CpuDetails = await _cpuScanner.ScanDetailsAsync(); }
        catch { /* 扫描失败时保留旧列表 */ }
    }

    /// <summary>结束明细列表中选中的一组进程（同名合并项会结束全部实例）。返回实际结束的个数。</summary>
    public Task<int> EndCpuProcessesAsync(IReadOnlyList<int> pids) => Task.Run(() =>
        pids.Count(pid => CpuUsageScanner.EndProcess(pid)));

    private void OnSnapshot(SystemSnapshot snapshot)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CpuUsage = snapshot.CpuUsage;
            MemoryUsage = snapshot.MemoryUsage;
            MemoryText = $"{ByteFormatter.Format((double)snapshot.MemoryUsedBytes)} / {ByteFormatter.Format((double)snapshot.MemoryTotalBytes)}";
            MemoryInfoText = $"当前使用率 {snapshot.MemoryUsage:0.#}%";
            Disks = snapshot.Disks;
            UpdateCDriveSummary(snapshot.Disks);
        });
    }

    /// <summary>C盘清理入口卡片的状态文本：显示系统盘剩余空间，随每秒快照自动刷新。</summary>
    private void UpdateCDriveSummary(IReadOnlyList<DiskUsageInfo> disks)
    {
        string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))?.TrimEnd('\\') ?? "C:";
        var disk = disks.FirstOrDefault(d => string.Equals(d.Name, root, StringComparison.OrdinalIgnoreCase));
        CDriveSummaryText = disk is null ? "" : $"C 盘剩余 {ByteFormatter.Format(disk.FreeBytes)}";
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
        bool doTemp = IsTempSelected, doDeep = IsDeepSelected, doMemory = IsMemorySelected, doCpuCool = IsCpuCoolSelected;
        int steps = (doTemp ? 1 : 0) + (doDeep ? 1 : 0) + (doMemory ? 1 : 0) + (doCpuCool ? 1 : 0);
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

            if (doCpuCool)
            {
                ProgressText = "正在为 CPU 降温…";
                var cool = await _cpuScanner.CoolAsync();
                ProgressValue = ++done * 100.0 / steps;
                if (cool.Killed > 0 || cool.Demoted > 0)
                {
                    var parts = new List<string>();
                    if (cool.Killed > 0) parts.Add($"结束 {cool.Killed} 个高占用后台进程");
                    if (cool.Demoted > 0) parts.Add($"{cool.Demoted} 个程序已降速");
                    summary.Add("CPU 降温：" + string.Join("，", parts));
                }
                else
                {
                    summary.Add("CPU 降温：没有发现需要处理的高占用程序");
                }
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
