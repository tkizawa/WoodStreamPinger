using System.Diagnostics;
using System.Globalization;
using System.Text;
using WoodStreamPinger.Models;

namespace WoodStreamPinger.Services;

/// <summary>
/// Ping 実行結果を日別 CSV ファイルへスレッドセーフに追記するサービス
/// </summary>
public sealed class PingLogService : IPingLogService
{
    private readonly object _fileLock = new();

    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public string DefaultLogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "WoodStreamPinger",
        "Logs");

    /// <summary>
    /// 設定値が空の場合は既定フォルダ（ドキュメント\WoodStreamPinger\Logs）を返す
    /// </summary>
    public string ResolveLogDirectory(string? configuredDirectory)
    {
        return string.IsNullOrWhiteSpace(configuredDirectory)
            ? DefaultLogDirectory
            : configuredDirectory.Trim();
    }

    /// <summary>
    /// 1 回の Ping 実行結果を日別 CSV ファイル（WoodStreamPinger_Log_yyyyMMdd.csv）へ追記
    /// 形式: 実行時間,宛先,応答時間(ms),TTL
    /// </summary>
    public void WriteEntry(string host, PingResultItem result, string? logDirectory, string csvHeader)
    {
        // キャンセル（Idle）は実行ログに記録しない
        if (result.Status == HostPingStatus.Idle)
        {
            return;
        }

        try
        {
            var directory = ResolveLogDirectory(logDirectory);
            var fileName = $"WoodStreamPinger_Log_{result.Timestamp:yyyyMMdd}.csv";
            var filePath = Path.Combine(directory, fileName);

            var timestampText = result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var hostEscaped = EscapeCsvField(host);
            var rttText = result.Status == HostPingStatus.Success
                ? result.RoundtripTimeMs.ToString(CultureInfo.InvariantCulture)
                : result.Status.ToString();
            var ttlText = result.Status == HostPingStatus.Success
                ? result.Ttl.ToString(CultureInfo.InvariantCulture)
                : "-";

            var line = $"{timestampText},{hostEscaped},{rttText},{ttlText}";

            lock (_fileLock)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool isNewFile = !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

                // 読み取り専用ツール等で開かれていても追記できるよう FileShare.ReadWrite を指定
                using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, isNewFile ? Utf8WithBom : Utf8WithoutBom);

                if (isNewFile)
                {
                    writer.WriteLine(csvHeader);
                }

                writer.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            // ファイルロックやアクセス権限エラー等で Ping 実行自体を止めない
            Debug.WriteLine($"[PingLogService] ログ書き込みエラー: {ex.Message}");
        }
    }

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
