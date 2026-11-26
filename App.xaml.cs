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
    public static Window? MainWindowInstance { get; private set; }

    public static ISettingsService SettingsService { get; } = new SettingsService();
    public static IPingService PingService { get; } = new PingService();
    public static IPingLogService PingLogService { get; } = new PingLogService();
    public static LocalizationService LocalizationService => LocalizationService.Instance;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "WoodStreamPinger", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.WriteAllText(logPath, $"AppDomain UnhandledException:\n{e.ExceptionObject}");
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "WoodStreamPinger", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.WriteAllText(logPath, $"TaskScheduler UnobservedTaskException:\n{e.Exception}");
            }
            catch { }
        };

        UnhandledException += (s, e) =>
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "WoodStreamPinger", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.WriteAllText(logPath, $"Xaml UnhandledException:\n{e.Message}\n{e.Exception}\nStackTrace:\n{e.Exception?.StackTrace}");
            }
            catch { }
        };

        InitializeComponent();
    }

    /// <summary>
    /// アプリケーション起動時の初期化
    /// </summary>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // 設定の読み込み
            var settings = await SettingsService.LoadSettingsAsync();

            // 言語設定の適用
            LocalizationService.SetLanguage(settings.Language);

            _window = new MainWindow();
            MainWindowInstance = _window;
            _window.Activate();
        }
        catch (System.Exception ex)
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "WoodStreamPinger", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.WriteAllText(logPath, $"OnLaunched Exception:\n{ex.Message}\n{ex}\nStackTrace:\n{ex.StackTrace}");
            }
            catch { }
            throw;
        }
    }
}
