using System.Collections.ObjectModel;
using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.ViewModels;

public sealed class BlueScreenViewModel : ViewModelBase
{
    public ObservableCollection<BlueScreenEvent> Items { get; } = new();

    private string _emptyText = "";
    public string EmptyText { get => _emptyText; set => SetProperty(ref _emptyText, value); }

    public BlueScreenViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var events = await new BlueScreenAnalyzer().GetEventsAsync();
        foreach (var item in events) Items.Add(item);
        EmptyText = Items.Count == 0 ? "未发现蓝屏记录，系统运行良好 ✔" : "";
    }
}
