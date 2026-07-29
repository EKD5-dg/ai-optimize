using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
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

    public BigFilesViewModel()
    {
        _ = ScanAsync();
    }

    private async Task ScanAsync()
    {
        var files = await BigFileScanner.ScanAsync();
        foreach (var file in files) Items.Add(new BigFileItemViewModel(this, file));
        IsScanning = false;
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
        OpenLocationCommand = new RelayCommand(_ => OpenLocation());
        DeleteCommand = new RelayCommand(_ => Delete());
    }

    public string Name => _file.Name;
    public string Directory => _file.Directory;
    public long Bytes => _file.Bytes;

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
        var confirm = MessageBox.Show(
            $"确定把这个文件放入回收站吗？\n\n{_file.Name}（{ByteFormatter.Format(_file.Bytes)}）\n位置：{_file.Directory}\n\n放入回收站后仍可恢复；清空回收站后才会真正释放空间。",
            "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
