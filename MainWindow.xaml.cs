using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WoodStreamPinger;

using Microsoft.UI.Xaml;
using WoodStreamPinger.Helpers;

/// <summary>
/// メインウィンドウ。ウィンドウの位置・サイズ復元および保存を制御。
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // アイコンの設定
        AppWindow.SetIcon("Assets/AppIcon.ico");

        var settings = App.SettingsService.CurrentSettings;

        // ウィンドウ位置およびサイズの復元（規約準拠）
        WindowHelper.RestoreWindowBounds(this, settings);

        // ウィンドウ終了時の位置・サイズ保存
        Closed += OnWindowClosed;

        // メインページの表示
        RootFrame.Navigate(typeof(MainPage));
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        var settings = App.SettingsService.CurrentSettings;
        WindowHelper.SaveWindowBounds(this, settings);
        _ = App.SettingsService.SaveSettingsAsync(settings);
    }
}
