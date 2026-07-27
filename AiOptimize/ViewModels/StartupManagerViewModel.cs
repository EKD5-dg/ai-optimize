using System.Collections.ObjectModel;
using System.Windows;
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
    }

    public string Name => _item.Name;
    public string Command => _item.Command;
    public string SourceDisplay => _item.SourceDisplay;

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
