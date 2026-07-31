using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.ViewModels;

public sealed class StartupManagerViewModel : ViewModelBase
{
    public ObservableCollection<StartupItemViewModel> Items { get; } = new();

    public StartupManagerViewModel()
    {
        var manager = new StartupManager();
        try
        {
            foreach (var item in manager.GetItems())
            {
                Items.Add(new StartupItemViewModel(manager, item));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"读取启动项失败：{ex.Message}", "启动项管理",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

public sealed class StartupItemViewModel : ViewModelBase
{
    private readonly StartupManager _manager;
    private readonly StartupItem _item;

    public StartupItemViewModel(StartupManager manager, StartupItem item)
    {
        _manager = manager;
        _item = item;
        Info = StartupKnowledge.Describe(item.Name, item.Command);
    }

    public string Name => _item.Name;
    public string Command => CommandPathFormatter.Clean(_item.Command);
    public string SourceDisplay => _item.SourceDisplay;

    public StartupInfo Info { get; }
    public string Description => Info.Description;

    public string AdviceLabel => Info.Advice switch
    {
        StartupAdvice.Keep => "建议保留",
        StartupAdvice.Optional => "可关闭",
        _ => "自行判断",
    };

    public Brush AdviceBrush => Info.Advice switch
    {
        StartupAdvice.Keep => new SolidColorBrush(Color.FromRgb(0x7C, 0xE3, 0x8B)),
        StartupAdvice.Optional => new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
        _ => new SolidColorBrush(Color.FromRgb(0xE8, 0xB4, 0x4C)),
    };

    public bool IsEnabled
    {
        get => _item.IsEnabled;
        set
        {
            if (_item.IsEnabled == value) return;
            try
            {
                _manager.SetEnabled(_item, value);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "启动项管理",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            OnPropertyChanged();
        }
    }
}
