using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WoodStreamPinger.Models;
using WoodStreamPinger.Services;

namespace WoodStreamPinger.Helpers;

/// <summary>
/// ウィンドウの位置・サイズ保存および復元、テーマ制御を補助するヘルパー
/// </summary>
public static class WindowHelper
{
    /// <summary>
    /// 設定からウィンドウの位置とサイズを復元
    /// </summary>
    public static void RestoreWindowBounds(Window window, AppSettings settings)
    {
        var appWindow = window.AppWindow;
        if (appWindow == null) return;

        // 幅・高さの安全チェック
        int width = Math.Max(800, settings.WindowWidth);
        int height = Math.Max(500, settings.WindowHeight);

        // 前回保存された位置が存在する場合
        if (settings.WindowX >= 0 && settings.WindowY >= 0)
        {
            // ディスプレイ範囲外に消えないようチェック
            var displayArea = DisplayArea.GetFromPoint(
                new PointInt32(settings.WindowX + 50, settings.WindowY + 50),
                DisplayAreaFallback.Primary);

            if (displayArea != null)
            {
                appWindow.MoveAndResize(new RectInt32(settings.WindowX, settings.WindowY, width, height));
            }
            else
            {
                appWindow.Resize(new SizeInt32(width, height));
            }
        }
        else
        {
            appWindow.Resize(new SizeInt32(width, height));
        }

        if (settings.IsMaximized)
        {
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
    }

    /// <summary>
    /// 現在のウィンドウ位置とサイズを設定モデルに保存
    /// </summary>
    public static void SaveWindowBounds(Window window, AppSettings settings)
    {
        var appWindow = window.AppWindow;
        if (appWindow == null) return;

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            settings.IsMaximized = presenter.State == OverlappedPresenterState.Maximized;
        }

        // 最大化されている場合は最大化前の通常サイズを維持
        if (!settings.IsMaximized)
        {
            settings.WindowX = appWindow.Position.X;
            settings.WindowY = appWindow.Position.Y;
            settings.WindowWidth = appWindow.Size.Width;
            settings.WindowHeight = appWindow.Size.Height;
        }
    }

    /// <summary>
    /// テーマ（System / Light / Dark）をルートフレームに適用
    /// </summary>
    public static void ApplyTheme(FrameworkElement rootElement, string themeSetting)
    {
        rootElement.RequestedTheme = themeSetting switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default // システムのライト/ダーク設定に従う
        };
    }
}
