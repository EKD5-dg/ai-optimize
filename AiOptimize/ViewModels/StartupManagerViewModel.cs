using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.ViewModels;

public sealed class StartupManagerViewModel : ViewModelBase
{
    public ObservableCollection<StartupItemViewModel> Items { get; } = new();

    /// <summary>带搜索过滤的视图，界面绑定此属性。</summary>
    public ICollectionView ItemsView { get; }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value)) ItemsView.Refresh();
        }
    }

    public StartupManagerViewModel()
    {
        // 使用私有视图，避免与其他绑定共享默认视图的过滤器
        ItemsView = new CollectionViewSource { Source = Items }.View;
        ItemsView.Filter = o => o is StartupItemViewModel item &&
            StartupSearch.Matches(item.Name, item.Description, item.Command, SearchText);
        ItemsView.SortDescriptions.Add(new SortDescription(nameof(StartupItemViewModel.Name),
            ListSortDirection.Ascending));

        var manager = new StartupManager();
        try
        {
            // 先全部读到本地列表，成功后再一次性填充，避免中途异常留下半截数据
            var loaded = manager.GetItems().Select(item => new StartupItemViewModel(manager, item)).ToList();
            foreach (var vm in loaded) Items.Add(vm);
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

    private static readonly Brush KeepBrush = CreateFrozen(0x7C, 0xE3, 0x8B);
    private static readonly Brush OptionalBrush = CreateFrozen(0x4F, 0xC3, 0xF7);
    private static readonly Brush DefaultBrush = CreateFrozen(0xE8, 0xB4, 0x4C);

    private static Brush CreateFrozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public Brush AdviceBrush => Info.Advice switch
    {
        StartupAdvice.Keep => KeepBrush,
        StartupAdvice.Optional => OptionalBrush,
        _ => DefaultBrush,
    };

    private bool _isToggling;

    public bool IsEnabled
    {
        get => _item.IsEnabled;
        set
        {
            // 防重入：绑定回写与命令并发时只执行一次注册表写入
            if (_item.IsEnabled == value || _isToggling) return;
            _isToggling = true;
            try
            {
                _manager.SetEnabled(_item, value);
            }
            catch (System.Security.SecurityException ex)
            {
                ShowError($"没有权限修改该启动项：{ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowError($"没有权限修改该启动项：{ex.Message}");
            }
            catch (System.IO.IOException ex)
            {
                ShowError($"操作失败：{ex.Message}");
            }
            finally
            {
                _isToggling = false;
                // 无论成败都刷新绑定：失败时开关弹回真实状态
                OnPropertyChanged();
            }
        }
    }

    private static void ShowError(string message)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (owner != null)
            MessageBox.Show(owner, message, "启动项管理", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            MessageBox.Show(message, "启动项管理", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
