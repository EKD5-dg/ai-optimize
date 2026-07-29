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
                if (type == QuickActionType.DiskCheck)
                {
                    // 磁盘检查用程序自己的友好窗口，而非命令行
                    var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                    new DiskCheckWindow { Owner = owner }.ShowDialog();
                    return;
                }
                QuickActionCatalog.Launch(type);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}", "蓝屏分析",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        });
    }
}
