namespace WoodStreamPinger.Models;

/// <summary>
/// 1 回の Ping 試行結果を表すレコード
/// </summary>
public sealed class PingResultItem
{
    /// <summary>
    /// タイムスタンプ
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// Ping 実行結果ステータス
    /// </summary>
    public HostPingStatus Status { get; init; }

    /// <summary>
    /// 往復応答時間（ミリ秒）
    /// </summary>
    public long RoundtripTimeMs { get; init; }

    /// <summary>
    /// 生存可能時間 (TTL)
    /// </summary>
    public int Ttl { get; init; }

    /// <summary>
    /// バイト数
    /// </summary>
    public int BufferSize { get; init; }

    /// <summary>
    /// 詳細メッセージまたはエラー内容
    /// </summary>
    public string? Message { get; init; }
}
