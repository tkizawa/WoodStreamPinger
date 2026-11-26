using WoodStreamPinger.Models;

namespace WoodStreamPinger.Services;

/// <summary>
/// Ping 実行結果を 1 回ごとに CSV ログファイルへ記録するサービスインターフェース
/// </summary>
public interface IPingLogService
{
    /// <summary>
    /// 既定のログ保存先フォルダパスを取得
    /// </summary>
    string DefaultLogDirectory { get; }

    /// <summary>
    /// 指定された保存先フォルダ（未指定時は既定フォルダ）を解決して返す
    /// </summary>
    string ResolveLogDirectory(string? configuredDirectory);

    /// <summary>
    /// 1 回の Ping 実行結果を CSV ファイルへ 1 行追記（実行時間, 宛先, 応答時間(ms), TTL）
    /// </summary>
    void WriteEntry(string host, PingResultItem result, string? logDirectory, string csvHeader);
}
