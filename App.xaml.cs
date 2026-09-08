using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WoodStreamPinger;

using Microsoft.UI.Xaml;
using WoodStreamPinger.Services;

/// <summary>
/// アプリケーションのエントリポイントおよびサービスプロバイダ
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public static ISettingsService SettingsService { get; } = new SettingsService();
    public static IPingService PingService { get; } = new PingService();
    public static LocalizationService LocalizationService => LocalizationService.Instance;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// アプリケーション起動時の初期化
    /// </summary>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 設定の読み込み
        var settings = await SettingsService.LoadSettingsAsync();

        // 言語設定の適用
        LocalizationService.SetLanguage(settings.Language);

        _window = new MainWindow();
        _window.Activate();
    }
}
