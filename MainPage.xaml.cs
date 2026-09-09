using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WoodStreamPinger.Helpers;
using WoodStreamPinger.Models;
using WoodStreamPinger.ViewModels;

namespace WoodStreamPinger;

/// <summary>
/// アプリケーションのメイン操作画面
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();

        ViewModel = new MainViewModel(
            App.PingService,
            App.SettingsService,
            DispatcherQueue);

        DataContext = ViewModel;

        Loaded += OnLoaded;
    }

    private bool _isLoaded = false;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 保存されていたテーマの適用
            var settings = App.SettingsService.CurrentSettings;
            WindowHelper.ApplyTheme(this, settings.Theme);

            // テーマ選択UIの初期選択
            if (ThemeComboBox != null)
            {
                ThemeComboBox.SelectedIndex = settings.Theme switch
                {
                    "Light" => 1,
                    "Dark" => 2,
                    _ => 0 // System
                };
            }

            // 言語選択UIの初期選択
            if (LanguageComboBox != null)
            {
                LanguageComboBox.SelectedIndex = settings.Language switch
                {
                    "ja-JP" => 1,
                    "en-US" => 2,
                    _ => 0 // Auto
                };
            }

            _isLoaded = true;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// テーマの変更（システム / ライト / ダーク）
    /// </summary>
    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;
        var cb = sender as ComboBox ?? ThemeComboBox;
        if (cb?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            WindowHelper.ApplyTheme(this, tag);

            var settings = App.SettingsService.CurrentSettings;
            settings.Theme = tag;
            _ = App.SettingsService.SaveSettingsAsync(settings);
        }
    }

    /// <summary>
    /// 表示言語の変更（自動 / 日本語 / 英語）
    /// </summary>
    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;
        var cb = sender as ComboBox ?? LanguageComboBox;
        if (cb?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.Loc.SetLanguage(tag);

            var settings = App.SettingsService.CurrentSettings;
            settings.Language = tag;
            _ = App.SettingsService.SaveSettingsAsync(settings);
        }
    }

    /// <summary>
    /// ホスト一括追加ダイアログの表示
    /// </summary>
    private async void OnAddHostClicked(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox
        {
            AcceptsReturn = true,
            Height = 160,
            PlaceholderText = ViewModel.Loc["Dialog_AddHost_Placeholder"]
        };

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = ViewModel.Loc["Dialog_AddHost_Title"],
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = ViewModel.Loc["Dialog_AddHost_Description"], TextWrapping = TextWrapping.Wrap },
                    textBox
                }
            },
            PrimaryButtonText = ViewModel.Loc["Dialog_Add"],
            CloseButtonText = ViewModel.Loc["Dialog_Cancel"],
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            ViewModel.AddHosts(textBox.Text);
        }
    }

    private void OnStartItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HostPingTarget target })
        {
            ViewModel.StartTarget(target);
        }
    }

    private void OnStopItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HostPingTarget target })
        {
            ViewModel.StopTarget(target);
        }
    }

    private void OnRemoveItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HostPingTarget target })
        {
            ViewModel.RemoveTarget(target);
        }
    }

    /// <summary>
    /// 統計結果の CSV エクスポート処理
    /// </summary>
    private async void OnExportCsvClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Targets.Count == 0)
        {
            var noDataDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = ViewModel.Loc.GetString("Toolbar_ExportCsv"),
                Content = ViewModel.Loc.GetString("Export_NoData"),
                CloseButtonText = ViewModel.Loc.GetString("Dialog_Close")
            };
            await noDataDialog.ShowAsync();
            return;
        }

        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"WoodStreamPinger_Stats_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            savePicker.FileTypeChoices.Add("CSV (カンマ区切り) (*.csv)", new List<string> { ".csv" });

            // WinUI 3 デスクトップアプリ用 HWND 初期化
            var window = App.MainWindowInstance;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await ViewModel.ExportCsvAsync(file.Path);

                var successDialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = ViewModel.Loc.GetString("Export_Success_Title"),
                    Content = string.Format(ViewModel.Loc.GetString("Export_Success_Message"), file.Path),
                    CloseButtonText = ViewModel.Loc.GetString("Dialog_Close")
                };
                await successDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Error",
                Content = ex.Message,
                CloseButtonText = ViewModel.Loc.GetString("Dialog_Close")
            };
            await errorDialog.ShowAsync();
        }
    }
}
