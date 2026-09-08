using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WoodStreamPinger.Models;

namespace WoodStreamPinger.Converters;

/// <summary>
/// HostPingStatus をステータスバッジの背景ブラシに変換
/// </summary>
public sealed class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush IdleBrush = new(Color.FromArgb(255, 108, 117, 125));       // グレー
    private static readonly SolidColorBrush RunningBrush = new(Color.FromArgb(255, 13, 110, 253));     // ブルー
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromArgb(255, 25, 135, 84));       // グリーン
    private static readonly SolidColorBrush TimeoutBrush = new(Color.FromArgb(255, 253, 126, 20));     // オレンジ
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromArgb(255, 220, 53, 69));         // レッド

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is HostPingStatus status)
        {
            return status switch
            {
                HostPingStatus.Success => SuccessBrush,
                HostPingStatus.Running => RunningBrush,
                HostPingStatus.Timeout => TimeoutBrush,
                HostPingStatus.DestinationUnreachable => ErrorBrush,
                HostPingStatus.Error => ErrorBrush,
                _ => IdleBrush
            };
        }

        return IdleBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

/// <summary>
/// RTT (ms) を見やすさに応じたテキストブラシに変換
/// </summary>
public sealed class RttToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FastBrush = new(Color.FromArgb(255, 25, 135, 84));    // < 50ms (緑)
    private static readonly SolidColorBrush NormalBrush = new(Color.FromArgb(255, 40, 167, 69));  // < 100ms
    private static readonly SolidColorBrush SlowBrush = new(Color.FromArgb(255, 253, 126, 20));   // < 200ms (オレンジ)
    private static readonly SolidColorBrush BadBrush = new(Color.FromArgb(255, 220, 53, 69));     // >= 200ms (赤)
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 108, 117, 125));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long rtt)
        {
            if (rtt < 0) return DefaultBrush;
            if (rtt <= 50) return FastBrush;
            if (rtt <= 100) return NormalBrush;
            if (rtt <= 200) return SlowBrush;
            return BadBrush;
        }

        if (value is double dRtt)
        {
            if (dRtt <= 0) return DefaultBrush;
            if (dRtt <= 50) return FastBrush;
            if (dRtt <= 100) return NormalBrush;
            if (dRtt <= 200) return SlowBrush;
            return BadBrush;
        }

        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

/// <summary>
/// 損失率 (%) をアラートカラーに変換
/// </summary>
public sealed class LossRateToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush GoodBrush = new(Color.FromArgb(255, 25, 135, 84));
    private static readonly SolidColorBrush WarnBrush = new(Color.FromArgb(255, 253, 126, 20));
    private static readonly SolidColorBrush BadBrush = new(Color.FromArgb(255, 220, 53, 69));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double loss)
        {
            if (loss <= 0.0) return GoodBrush;
            if (loss < 10.0) return WarnBrush;
            return BadBrush;
        }

        return GoodBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
