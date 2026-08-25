using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AiOptimize.Models;
using AiOptimize.Services;
using AiOptimize.Utils;

namespace AiOptimize.ViewModels;

public sealed class CDriveCleanViewModel : ViewModelBase
{
    private readonly CDriveCleaner _cleaner = new();
    private readonly CancellationTokenSource _cts = new();

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = new();

    private string _statusText = "正在扫描 C 盘可清理空间…";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                CleanCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>供复选框 IsEnabled 反向绑定：扫描或清理进行中禁止改动选择。</summary>
    public bool IsNotBusy => !IsBusy;

    private string _resultText = "";
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    private string _driveTotalText = "";
    public string DriveTotalText { get => _driveTotalText; set => SetProperty(ref _driveTotalText, value); }

    private string _driveUsedText = "";
    public string DriveUsedText { get => _driveUsedText; set => SetProperty(ref _driveUsedText, value); }

    private string _driveFreeText = "";
    public string DriveFreeText { get => _driveFreeText; set => SetProperty(ref _driveFreeText, value); }

    private double _driveUsage;
    public double DriveUsage { get => _driveUsage; set => SetProperty(ref _driveUsage, value); }

    public AsyncRelayCommand CleanCommand { get; }

    public CDriveCleanViewModel()
    {
        CleanCommand = new AsyncRelayCommand(
            async _ => await CleanAsync(),
            _ => !IsBusy && Categories.Any(c => c.IsChecked),
            ex => ResultText = $"清理过程中出现问题：{ex.Message}");
        _ = ScanAsync();
    }

    /// <summary>窗口关闭时取消正在进行的清理（DISM 等外部命令会被终止）。</summary>
    public void Cancel() => _cts.Cancel();

    internal void OnSelectionChanged() => CleanCommand.RaiseCanExecuteChanged();

    private async Task ScanAsync()
    {
        // 扫描期间保持忙碌：避免分类列表尚未就绪时就能勾选或点击“开始清理”
        IsBusy = true;
        try
        {
            var driveTask = Task.Run(QuerySystemDrive);
            var categories = await _cleaner.ScanAsync();
            Categories.Clear();
            foreach (var category in categories)
                Categories.Add(new CategoryItemViewModel(category, OnSelectionChanged));
            UpdateDriveInfo(await driveTask);

            long total = categories.Where(c => c.Bytes > 0).Sum(c => c.Bytes);
            StatusText = total > 0
                ? $"共发现约 {ByteFormatter.Format(total)} 可清理空间，勾选项目后点击下方按钮开始"
                : "没有发现明显的垃圾文件。";
        }
        catch (Exception ex)
        {
            StatusText = $"扫描失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CleanAsync()
    {
        var selected = Categories.Where(c => c.IsChecked).Select(c => c.Category).ToList();
        if (selected.Count == 0) return;

        var systemLevel = selected.Where(c => !c.IsCheckedByDefault).ToList();
        if (systemLevel.Count > 0)
        {
            string warnings = string.Join("\n\n",
                systemLevel.Select(c => $"⚠ {c.Name}：{c.Description}"));
            var confirm = MessageBox.Show(
                $"以下项目会影响系统功能，确定继续吗？\n\n{warnings}",
                "确认执行系统级清理", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        ResultText = "";
        IsBusy = true;
        var total = new CleanResult();
        try
        {
            foreach (var category in selected)
            {
                StatusText = $"正在处理：{category.Name}…";
                total.Merge(await _cleaner.CleanCategoryAsync(category.Id, _cts.Token));
            }

            StatusText = "清理完成，正在重新扫描…";
            var notes = total.Notes.Distinct().ToList();
            var summary = new List<string> { $"✔ 共释放磁盘空间 {ByteFormatter.Format(total.BytesFreed)}" };
            if (total.FilesSkipped > 0) summary.Add($"跳过 {total.FilesSkipped} 个被占用文件");
            summary.AddRange(notes);
            ResultText = string.Join("\n", summary);
        }
        catch (OperationCanceledException)
        {
            ResultText = "已取消。已完成的清理会保留。";
        }
        finally
        {
            IsBusy = false;
            await ScanAsync();
        }
    }

    private void UpdateDriveInfo((long Total, long Free) drive)
    {
        if (drive.Total <= 0) return;
        long used = drive.Total - drive.Free;
        DriveTotalText = ByteFormatter.Format(drive.Total);
        DriveUsedText = ByteFormatter.Format(used);
        DriveFreeText = ByteFormatter.Format(drive.Free);
        DriveUsage = used * 100.0 / drive.Total;
    }

    private static (long Total, long Free) QuerySystemDrive()
    {
        string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? @"C:\";
        try
        {
            var drive = new DriveInfo(root);
            return (drive.TotalSize, drive.AvailableFreeSpace);
        }
        catch
        {
            return (0, 0);
        }
    }
}

/// <summary>分类项的可绑定包装：CleanupCategory 是不可变 record，勾选状态放在这一层。</summary>
public sealed class CategoryItemViewModel : ViewModelBase
{
    private readonly Action _selectionChanged;
    private bool _isChecked;

    public CategoryItemViewModel(CleanupCategory category, Action selectionChanged)
    {
        Category = category;
        _selectionChanged = selectionChanged;
        _isChecked = category.IsCheckedByDefault;
    }

    public CleanupCategory Category { get; }
    public string Name => Category.Name;
    public string Description => Category.Description;

    /// <summary>Bytes 为 -1 表示无法预估（如 DISM 组件存储）。</summary>
    public string SizeText => Category.Bytes < 0 ? "清理后可见" : ByteFormatter.Format(Category.Bytes);

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value)) _selectionChanged();
        }
    }
}
