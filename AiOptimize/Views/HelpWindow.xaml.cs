using System.Reflection;
using System.Windows;
using AiOptimize.Services;

namespace AiOptimize.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        VersionTextBlock.Text = $"当前版本 v{version}";
    }

    /// <summary>手动检查更新：有新版本弹更新窗口，没有则提示已是最新。</summary>
    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        CheckUpdateButton.Content = "检查中…";
        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
            var update = await UpdateService.CheckLatestAsync(currentVersion);
            if (update is null)
            {
                MessageBox.Show($"当前已是最新版本（v{currentVersion.ToString(3)}）。", "软件更新",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var window = new UpdateWindow(update) { Owner = this };
                window.ShowDialog();
            }
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
            CheckUpdateButton.Content = "检查更新";
        }
    }
}
