using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace WoodStreamPinger.Controls;

/// <summary>
/// Ping の OK（青）/ NG（赤）回数をドーナツ円グラフで可視化するカスタムコントロール
/// </summary>
public sealed partial class DonutChartControl : UserControl
{
    public static readonly DependencyProperty OkCountProperty =
        DependencyProperty.Register(
            nameof(OkCount),
            typeof(int),
            typeof(DonutChartControl),
            new PropertyMetadata(0, OnChartPropertyChanged));

    public static readonly DependencyProperty NgCountProperty =
        DependencyProperty.Register(
            nameof(NgCount),
            typeof(int),
            typeof(DonutChartControl),
            new PropertyMetadata(0, OnChartPropertyChanged));

    public static readonly DependencyProperty ChartSizeProperty =
        DependencyProperty.Register(
            nameof(ChartSize),
            typeof(double),
            typeof(DonutChartControl),
            new PropertyMetadata(148.0, OnChartPropertyChanged));

    public static readonly DependencyProperty RingThicknessProperty =
        DependencyProperty.Register(
            nameof(RingThickness),
            typeof(double),
            typeof(DonutChartControl),
            new PropertyMetadata(18.0, OnChartPropertyChanged));

    public static readonly DependencyProperty TotalLabelFormatProperty =
        DependencyProperty.Register(
            nameof(TotalLabelFormat),
            typeof(string),
            typeof(DonutChartControl),
            new PropertyMetadata("Total: {0}", OnChartPropertyChanged));

    /// <summary>
    /// OK（成功）回数（青色で表示）
    /// </summary>
    public int OkCount
    {
        get => (int)GetValue(OkCountProperty);
        set => SetValue(OkCountProperty, value);
    }

    /// <summary>
    /// NG（失敗・タイムアウト等）回数（赤色で表示）
    /// </summary>
    public int NgCount
    {
        get => (int)GetValue(NgCountProperty);
        set => SetValue(NgCountProperty, value);
    }

    /// <summary>
    /// グラフ外径（px）
    /// </summary>
    public double ChartSize
    {
        get => (double)GetValue(ChartSizeProperty);
        set => SetValue(ChartSizeProperty, value);
    }

    /// <summary>
    /// ドーナツリングの太さ（px）
    /// </summary>
    public double RingThickness
    {
        get => (double)GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    /// <summary>
    /// 中央サブテキストのフォーマット（例: "合計: {0}" / "Total: {0}"）
    /// </summary>
    public string TotalLabelFormat
    {
        get => (string)GetValue(TotalLabelFormatProperty);
        set => SetValue(TotalLabelFormatProperty, value);
    }

    public DonutChartControl()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateChart();
    }

    private static void OnChartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChartControl control)
        {
            control.UpdateChart();
        }
    }

    /// <summary>
    /// OK / NG の回数に応じてドーナツ円グラフのパスと中央テキストを更新
    /// </summary>
    private void UpdateChart()
    {
        if (OkArcPath == null || NgArcPath == null)
        {
            return;
        }

        int ok = Math.Max(0, OkCount);
        int ng = Math.Max(0, NgCount);
        int total = ok + ng;

        // 中央のテキスト更新
        string format = string.IsNullOrEmpty(TotalLabelFormat) ? "{0}" : TotalLabelFormat;
        CenterSubText.Text = string.Format(format, total);

        if (total == 0)
        {
            CenterPercentText.Text = "--%";
            OkFullEllipse.Visibility = Visibility.Collapsed;
            NgFullEllipse.Visibility = Visibility.Collapsed;
            OkArcPath.Visibility = Visibility.Collapsed;
            NgArcPath.Visibility = Visibility.Collapsed;
            return;
        }

        double okRate = (double)ok / total * 100.0;
        CenterPercentText.Text = $"{okRate:0.0}%";

        // 100% OK または 100% NG の場合はフルリングの Ellipse を使用
        if (ng == 0)
        {
            OkFullEllipse.Visibility = Visibility.Visible;
            NgFullEllipse.Visibility = Visibility.Collapsed;
            OkArcPath.Visibility = Visibility.Collapsed;
            NgArcPath.Visibility = Visibility.Collapsed;
            return;
        }

        if (ok == 0)
        {
            OkFullEllipse.Visibility = Visibility.Collapsed;
            NgFullEllipse.Visibility = Visibility.Visible;
            OkArcPath.Visibility = Visibility.Collapsed;
            NgArcPath.Visibility = Visibility.Collapsed;
            return;
        }

        // OK と NG が混在する場合は 12 時方向から時計回りにそれぞれの弧を描画
        OkFullEllipse.Visibility = Visibility.Collapsed;
        NgFullEllipse.Visibility = Visibility.Collapsed;

        double size = ChartSize;
        double center = size / 2.0;
        double radius = Math.Max(1.0, (size - RingThickness) / 2.0);

        double okAngle = (double)ok / total * 360.0;

        // 描画破綻を防ぐため角度をクランプ
        okAngle = Math.Clamp(okAngle, 0.5, 359.5);
        double ngAngle = 360.0 - okAngle;

        OkArcPath.Data = CreateArcGeometry(center, radius, 0.0, okAngle);
        NgArcPath.Data = CreateArcGeometry(center, radius, okAngle, ngAngle);

        OkArcPath.Visibility = Visibility.Visible;
        NgArcPath.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 指定した開始角度（12時=0度）とスイープ角度（時計回り）の円弧 PathGeometry を生成
    /// </summary>
    private static PathGeometry CreateArcGeometry(double center, double radius, double startAngleDeg, double sweepAngleDeg)
    {
        var startPoint = PolarToCartesian(center, radius, startAngleDeg);
        var endPoint = PolarToCartesian(center, radius, startAngleDeg + sweepAngleDeg);

        var arcSegment = new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            IsLargeArc = sweepAngleDeg > 180.0,
            SweepDirection = SweepDirection.Clockwise
        };

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };
        figure.Segments.Add(arcSegment);

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    /// <summary>
    /// 極座標（12時方向を 0 度、時計回り）をキャンバス上の直交座標へ変換
    /// </summary>
    private static Point PolarToCartesian(double center, double radius, double angleDeg)
    {
        double angleRad = (angleDeg - 90.0) * Math.PI / 180.0;
        double x = center + radius * Math.Cos(angleRad);
        double y = center + radius * Math.Sin(angleRad);
        return new Point(x, y);
    }
}
