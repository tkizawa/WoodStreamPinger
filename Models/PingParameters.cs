namespace WoodStreamPinger.Models;

/// <summary>
/// Ping 実行時のパラメータ設定モデル
/// </summary>
public sealed class PingParameters
{
    /// <summary>
    /// タイムアウト時間（ミリ秒）
    /// </summary>
    public int TimeoutMs { get; set; } = 1000;

    /// <summary>
    /// 送信インターバル間隔（ミリ秒）
    /// </summary>
    public int IntervalMs { get; set; } = 1000;

    /// <summary>
    /// パケットバッファサイズ（バイト数、デフォルト 32 バイト）
    /// </summary>
    public int BufferSize { get; set; } = 32;

    /// <summary>
    /// 生存可能時間 (Time To Live: 1〜255)
    /// </summary>
    public int Ttl { get; set; } = 128;

    /// <summary>
    /// パケットのフラグメント（分割）を禁止するかどうか (Don't Fragment)
    /// </summary>
    public bool DontFragment { get; set; } = true;

    /// <summary>
    /// 試行回数 (0 は手動停止までの継続ループ)
    /// </summary>
    public int Count { get; set; } = 0;

    /// <summary>
    /// 継続ループモードかどうか (Count == 0 のとき true)
    /// </summary>
    public bool IsContinuous => Count <= 0;

    /// <summary>
    /// パラメータのクローンを作成
    /// </summary>
    public PingParameters Clone() => new()
    {
        TimeoutMs = TimeoutMs,
        IntervalMs = IntervalMs,
        BufferSize = BufferSize,
        Ttl = Ttl,
        DontFragment = DontFragment,
        Count = Count
    };
}
