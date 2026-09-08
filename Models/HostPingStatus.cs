namespace WoodStreamPinger.Models;

/// <summary>
/// 各ホストの Ping 実行ステータス
/// </summary>
public enum HostPingStatus
{
    /// <summary>
    /// 未実行・待機中
    /// </summary>
    Idle,

    /// <summary>
    /// Ping 送信中・応答待ち
    /// </summary>
    Running,

    /// <summary>
    /// 成功（応答あり）
    /// </summary>
    Success,

    /// <summary>
    /// 要求がタイムアウト
    /// </summary>
    Timeout,

    /// <summary>
    /// 宛先ホスト到達不能
    /// </summary>
    DestinationUnreachable,

    /// <summary>
    /// 名前解決失敗またはエラー
    /// </summary>
    Error
}
