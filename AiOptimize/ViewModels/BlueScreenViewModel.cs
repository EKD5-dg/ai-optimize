using System.Collections.ObjectModel;
using System.Windows;
using AiOptimize.Models;
using AiOptimize.Services;
using AiOptimize.Views;

namespace AiOptimize.ViewModels;

public sealed class BlueScreenViewModel : ViewModelBase
{
    public ObservableCollection<BlueScreenItemViewModel> Items { get; } = new();

    /// <summary>顶部诊断摘要的提示行（环境风险 + 历史统计）。</summary>
    public ObservableCollection<SummaryLineViewModel> SummaryLines { get; } = new();

    private string _emptyText = "正在读取系统日志…";
    public string EmptyText { get => _emptyText; set => SetProperty(ref _emptyText, value); }

    private bool _hasSummary;
    public bool HasSummary { get => _hasSummary; set => SetProperty(ref _hasSummary, value); }

    public BlueScreenViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            // 事件读取与上下文探测并行执行，互不阻塞
            var eventsTask = new BlueScreenAnalyzer().GetEventsAsync();
            var contextTask = BlueScreenContextProbe.ProbeAsync();
            await Task.WhenAll(eventsTask, contextTask);

            var events = eventsTask.Result;
            foreach (var item in events) Items.Add(new BlueScreenItemViewModel(item));
            EmptyText = Items.Count == 0 ? "未发现蓝屏记录，系统运行良好 ✔" : "";
            if (Items.Count > 0 || contextTask.Result.Hints.Count > 0)
            {
                BuildSummary(events, contextTask.Result);
            }
        }
        catch (Exception ex)
        {
            EmptyText = $"读取蓝屏记录失败：{ex.Message}";
        }
    }

    /// <summary>汇总环境风险提示 + 崩溃历史统计。</summary>
    private void BuildSummary(IReadOnlyList<BlueScreenEvent> events, BlueScreenContext context)
    {
        // 环境风险提示
        foreach (var hint in context.Hints)
        {
            SummaryLines.Add(new SummaryLineViewModel(hint.Text, hint.Severity));
        }

        // 历史统计：总量 + 同代码重复次数（重复出现 = 高嫌疑）
        if (events.Count > 0)
        {
            var byCode = events.GroupBy(e => e.StopCodeText)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Max(e => e.Time));
            foreach (var g in byCode)
            {
                bool repeated = g.Count() >= 2;
                SummaryLines.Add(new SummaryLineViewModel(
                    $"停止代码 {g.Key}（{g.First().Name}）共出现 {g.Count()} 次" +
                    (repeated ? "，反复出现说明是该代码对应的驱动/硬件持续有问题" : ""),
                    repeated ? ContextSeverity.High : ContextSeverity.Info));
            }
            SummaryLines.Add(new SummaryLineViewModel(
                $"共 {events.Count} 条蓝屏记录，最近一次：{events.Max(e => e.Time):yyyy/MM/dd HH:mm}。",
                ContextSeverity.Info));
        }

        HasSummary = SummaryLines.Count > 0;
    }
}

/// <summary>诊断摘要单行：按严重程度着色。</summary>
public sealed class SummaryLineViewModel
{
    public string Text { get; }
    public bool IsHigh { get; }
    public bool IsMedium { get; }

    public SummaryLineViewModel(string text, ContextSeverity severity)
    {
        Text = text;
        IsHigh = severity == ContextSeverity.High;
        IsMedium = severity == ContextSeverity.Medium;
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
        ParamHint = StopCodeKnowledge.GetParamHint(e.StopCode, e.Parameters);
        if (e.Parameters.Count > 0)
        {
            ParamsText = "参数: " + string.Join(", ", e.Parameters.Select(p => $"0x{p:X16}"));
        }
        DumpStateText = e.DumpPath is null
            ? ""
            : e.DumpFileExists
                ? "转储文件已保留，可定位崩溃驱动 ✔"
                : "⚠ 转储文件缺失（可能被清理），无法精确定位崩溃驱动";
        DumpMissing = e.DumpPath is not null && !e.DumpFileExists;
    }

    public DateTime Time => _e.Time;
    public string StopCodeText => _e.StopCodeText;
    public string Name => _e.Name;
    public string Cause => _e.Cause;
    public string Advice => _e.Advice;
    public string? DumpPath => _e.DumpPath;
    public string ParamsText { get; }
    public string? ParamHint { get; }
    public string DumpStateText { get; }
    public bool DumpMissing { get; }
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
                    case QuickActionType.DeviceManager:
                        new ProblemDeviceWindow { Owner = owner }.ShowDialog();
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
