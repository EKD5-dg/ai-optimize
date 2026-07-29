using System.Collections.ObjectModel;
using System.Windows;
using AiOptimize.Models;
using AiOptimize.Services;
using AiOptimize.Views;

namespace AiOptimize.ViewModels;

public sealed class BlueScreenViewModel : ViewModelBase
{
    public ObservableCollection<BlueScreenItemViewModel> Items { get; } = new();

    private string _emptyText = "";
    public string EmptyText { get => _emptyText; set => SetProperty(ref _emptyText, value); }

    public BlueScreenViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var events = await new BlueScreenAnalyzer().GetEventsAsync();
        foreach (var item in events) Items.Add(new BlueScreenItemViewModel(item));
        EmptyText = Items.Count == 0 ? "未发现蓝屏记录，系统运行良好 ✔" : "";
    }
}

/// <summary>单条蓝屏记录的展示模型，附带一键操作按钮。</summary>
public sealed class BlueScreenItemViewModel
{
    private readonly BlueScreenEvent _e;

    public BlueScreenItemViewModel(BlueScreenEvent e)
    {
        _e = e;
        Actions = e.Actions.Select(t => new QuickActionViewModel(t)).ToList();
    }

    public DateTime Time => _e.Time;
    public string StopCodeText => _e.StopCodeText;
    public string Name => _e.Name;
    public string Cause => _e.Cause;
    public string Advice => _e.Advice;
    public string? DumpPath => _e.DumpPath;
    public IReadOnlyList<QuickActionViewModel> Actions { get; }
}

public sealed class QuickActionViewModel
{
    public string Label { get; }
    public RelayCommand RunCommand { get; }

    public QuickActionViewModel(QuickActionType type)
    {
        Label = QuickActionCatalog.Get(type).Label;
        RunCommand = new RelayCommand(_ =>
        {
            try
            {
                var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                switch (type)
                {
                    case QuickActionType.DiskCheck:
                        // 磁盘检查用程序自己的友好窗口，而非命令行
                        new DiskCheckWindow { Owner = owner }.ShowDialog();
                        return;
                    case QuickActionType.SfcScan:
                        new SfcCheckWindow { Owner = owner }.ShowDialog();
                        return;
                    case QuickActionType.MemoryDiagnostic:
                        MessageBox.Show(
                            "即将打开 Windows 内存诊断。\n\n它会问你“立即重启检查”还是“下次开机时检查”：\n• 检查在开机前的蓝色界面进行，大约 10-20 分钟，完成后自动进入系统；\n• 请先保存好正在编辑的文件再选“立即重启”。",
                            "内存诊断说明", MessageBoxButton.OK, MessageBoxImage.Information);
                        QuickActionCatalog.Launch(type);
                        return;
                    default:
                        QuickActionCatalog.Launch(type);
                        return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}", "蓝屏分析",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        });
    }
}
