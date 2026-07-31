using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using AiOptimize.Models;
using AiOptimize.Services;
using AiOptimize.Utils;

namespace AiOptimize.ViewModels;

public sealed class BigFilesViewModel : ViewModelBase
{
    public ObservableCollection<BigFileItemViewModel> Items { get; } = new();

    private string _statusText = "正在扫描个人文件夹和数据盘，请稍候…";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isScanning = true;
    public bool IsScanning { get => _isScanning; set => SetProperty(ref _isScanning, value); }

    private readonly CancellationTokenSource _cts = new();

    public BigFilesViewModel()
    {
        _ = ScanAsync();
    }

    /// <summary>窗口关闭时取消全盘扫描。</summary>
    public void Cancel() => _cts.Cancel();

    private async Task ScanAsync()
    {
        try
        {
            var files = await BigFileScanner.ScanAsync(cancellationToken: _cts.Token);
            foreach (var file in files) Items.Add(new BigFileItemViewModel(this, file));
        }
        catch (OperationCanceledException)
        {
            StatusText = "扫描已取消。";
            return;
        }
        catch (Exception ex)
        {
            StatusText = $"扫描失败：{ex.Message}";
            return;
        }
        finally
        {
            IsScanning = false;
        }
        UpdateStatus();
    }

    internal void Remove(BigFileItemViewModel item)
    {
        Items.Remove(item);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusText = Items.Count == 0
            ? "没有找到 100 MB 以上的大文件。"
            : $"共找到 {Items.Count} 个大文件，合计 {ByteFormatter.Format(Items.Sum(i => i.Bytes))}。删除的文件会进入回收站，可随时恢复。";
    }
}

public sealed class BigFileItemViewModel
{
    private readonly BigFilesViewModel _owner;
    private readonly BigFile _file;

    public BigFileItemViewModel(BigFilesViewModel owner, BigFile file)
    {
        _owner = owner;
        _file = file;
        Kind = FileKindCatalog.Describe(file.FullPath);
        OpenLocationCommand = new RelayCommand(_ => OpenLocation());
        DeleteCommand = new RelayCommand(_ => Delete());
    }

    public string Name => _file.Name;
    public string Directory => _file.Directory;
    public long Bytes => _file.Bytes;

    public FileKindInfo Kind { get; }
    public string KindDescription => Kind.Description;

    public string RiskLabel => Kind.Risk switch
    {
        FileRisk.Safe => "可以删除",
        FileRisk.Danger => "谨慎删除",
        _ => "自行判断",
    };

    private static readonly Brush SafeBrush = CreateFrozen(0x7C, 0xE3, 0x8B);
    private static readonly Brush DangerBrush = CreateFrozen(0xEF, 0x53, 0x50);
    private static readonly Brush DefaultBrush = CreateFrozen(0xE8, 0xB4, 0x4C);

    private static Brush CreateFrozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public Brush RiskBrush => Kind.Risk switch
    {
        FileRisk.Safe => SafeBrush,
        FileRisk.Danger => DangerBrush,
        _ => DefaultBrush,
    };

    public RelayCommand OpenLocationCommand { get; }
    public RelayCommand DeleteCommand { get; }

    private void OpenLocation()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_file.FullPath}\"")
            {
                UseShellExecute = true,
            })?.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开位置失败：{ex.Message}", "大文件查找",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Delete()
    {
        string warning = Kind.Risk == FileRisk.Danger
            ? $"\n\n⚠ 特别提醒：{Kind.Description}！"
            : $"\n\n文件类型：{Kind.Description}。";
        var confirm = MessageBox.Show(
            $"确定把这个文件放入回收站吗？\n\n{_file.Name}（{ByteFormatter.Format(_file.Bytes)}）\n位置：{_file.Directory}{warning}\n\n放入回收站后仍可恢复；清空回收站后才会真正释放空间。",
            "删除确认", MessageBoxButton.YesNo,
            Kind.Risk == FileRisk.Danger ? MessageBoxImage.Warning : MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            FileRecycler.MoveToRecycleBin(_file.FullPath);
            _owner.Remove(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败：{ex.Message}\n文件可能正在被其他程序使用。", "大文件查找",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
