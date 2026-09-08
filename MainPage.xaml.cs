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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 保存されていたテーマの適用
        var settings = App.SettingsService.CurrentSettings;
        WindowHelper.ApplyTheme(this, settings.Theme);

        // テーマ選択UIの初期選択
        ThemeComboBox.SelectedIndex = settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0 // System
        };

        // 言語選択UIの初期選択
        LanguageComboBox.SelectedIndex = settings.Language switch
        {
            "ja-JP" => 1,
            "en-US" => 2,
            _ => 0 // Auto
        };
    }

    /// <summary>
    /// テーマの変更（システム / ライト / ダーク）
    /// </summary>
    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
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
        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
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
}
