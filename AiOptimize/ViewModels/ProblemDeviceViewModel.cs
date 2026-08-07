using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.ViewModels;

public sealed class ProblemDeviceViewModel : ViewModelBase
{
    public ObservableCollection<ProblemDeviceItemViewModel> Items { get; } = new();

    private string _statusText = "正在扫描设备…";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isScanning = true;
    public bool IsScanning { get => _isScanning; set => SetProperty(ref _isScanning, value); }

    public ProblemDeviceViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var devices = await Task.Run(() => ProblemDeviceScanner.Scan());
            foreach (var d in devices) Items.Add(new ProblemDeviceItemViewModel(d));
            StatusText = Items.Count == 0
                ? "✅ 未发现异常设备，所有硬件运行正常"
                : $"发现 {Items.Count} 个设备可能需要关注";
        }
        catch (Exception ex)
        {
            StatusText = $"扫描失败：{ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}

public sealed class ProblemDeviceItemViewModel
{
    private readonly ProblemDevice _device;

    public ProblemDeviceItemViewModel(ProblemDevice device)
    {
        _device = device;
        OpenDeviceManagerCommand = new RelayCommand(_ => OpenInDeviceManager());
        UpdateDriverCommand = new RelayCommand(_ => UpdateDriver());
    }

    public string Name => _device.Name;
    public string ProblemDescription => _device.ProblemDescription;
    public string Status => _device.Status;
    public RelayCommand OpenDeviceManagerCommand { get; }
    public RelayCommand UpdateDriverCommand { get; }

    private void OpenInDeviceManager()
    {
        try
        {
            // 打开设备管理器并定位到该设备
            Process.Start(new ProcessStartInfo
            {
                FileName = "devmgmt.msc",
                UseShellExecute = true,
            })?.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开设备管理器失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateDriver()
    {
        try
        {
            // 使用 pnputil 尝试更新驱动
            var psi = new ProcessStartInfo
            {
                FileName = "pnputil.exe",
                Arguments = "/scan-devices",
                UseShellExecute = true,
                Verb = "runas",
            };
            Process.Start(psi)?.Dispose();
            MessageBox.Show(
                "已触发系统扫描硬件改动。\n\n如果 Windows 找到新驱动，会自动下载安装。\n也可以点击「打开设备管理器」手动更新。",
                "驱动更新", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"更新驱动失败：{ex.Message}\n\n请尝试手动打开设备管理器更新。",
                "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
