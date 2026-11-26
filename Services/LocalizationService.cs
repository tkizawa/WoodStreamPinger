using System.ComponentModel;
using System.Globalization;

namespace WoodStreamPinger.Services;

/// <summary>
/// アプリケーション多言語化（日本語・英語）サービス
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _currentLanguage = "en-US";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); // 全プロパティ更新通知
            }
        }
    }

    public string this[string key] => GetString(key);

    private readonly Dictionary<string, string> _jaStrings = new(StringComparer.OrdinalIgnoreCase)
    {
        // タイトル・ヘッダー
        ["App_Title"] = "WoodStream Pinger",
        ["App_Subtitle"] = "マルチホスト並行 Ping & リアルタイムネットワーク統計ツール",
        ["App_Version"] = "Version 1.0.1.0",

        // ツールバー
        ["Toolbar_StartAll"] = "すべて開始",
        ["Toolbar_StopAll"] = "すべて停止",
        ["Toolbar_ResetStats"] = "統計リセット",
        ["Toolbar_AddHost"] = "ホスト追加",
        ["Toolbar_ExportCsv"] = "実行結果出力 (CSV)",
        ["Toolbar_Settings"] = "設定",
        ["Toolbar_SearchPlaceholder"] = "ホスト名またはIPで絞り込み...",

        // CSVエクスポート
        ["Export_Success_Title"] = "エクスポート完了",
        ["Export_Success_Message"] = "統計結果をCSVファイルに出力しました:\n{0}",
        ["Export_NoData"] = "出力対象のホストがありません。",

        // Ping 実行ログ (CSV)
        ["Log_CsvHeader"] = "実行時間,宛先,応答時間(ms),TTL",
        ["Log_Enable"] = "Ping実行ログを記録 (CSV)",
        ["Log_Folder"] = "ログ保存先フォルダ:",
        ["Log_Browse"] = "参照...",
        ["Log_OpenFolder"] = "フォルダを開く",
        ["Log_RecordingBadge"] = "ログ記録中",

        // パラメータ
        ["Param_Title"] = "Ping パラメータ設定",
        ["Param_Timeout"] = "タイムアウト (ms):",
        ["Param_Interval"] = "間隔 (ms):",
        ["Param_BufferSize"] = "バッファサイズ (Byte):",
        ["Param_Ttl"] = "TTL (Time To Live):",
        ["Param_DontFragment"] = "フラグメント禁止 (DF)",
        ["Param_Continuous"] = "手動停止まで継続実行",
        ["Param_Count"] = "試行回数 (回):",

        // グリッド・リスト項目
        ["Grid_Host"] = "ホスト名 / IP",
        ["Grid_ResolvedIp"] = "解決済みIP",
        ["Grid_Status"] = "ステータス",
        ["Grid_LastRtt"] = "直近RTT",
        ["Grid_Sent"] = "送信",
        ["Grid_Received"] = "受信",
        ["Grid_Lost"] = "損失",
        ["Grid_LossRate"] = "損失率",
        ["Grid_MinRtt"] = "最小RTT",
        ["Grid_MaxRtt"] = "最大RTT",
        ["Grid_AvgRtt"] = "平均RTT",
        ["Grid_Actions"] = "操作",
        ["Grid_Start"] = "開始",
        ["Grid_Stop"] = "停止",
        ["Grid_Delete"] = "削除",
        ["Grid_Reset"] = "リセット",

        // サマリーステータスバー
        ["Status_TotalHosts"] = "登録ホスト数:",
        ["Status_ActiveHosts"] = "実行中:",
        ["Status_TotalSent"] = "総送信数:",
        ["Status_TotalReceived"] = "総受信数:",
        ["Status_TotalLost"] = "総損失数:",
        ["Status_OverallLossRate"] = "総合損失率:",
        ["Status_OverallAvgRtt"] = "全体平均RTT:",

        // ダイアログ・設定画面
        ["Dialog_AddHost_Title"] = "ホストの追加",
        ["Dialog_AddHost_Description"] = "ホスト名またはIPアドレスを入力してください（改行またはカンマ区切りで複数一括入力可）:",
        ["Dialog_AddHost_Placeholder"] = "例:\n8.8.8.8\n1.1.1.1\ngoogle.com\n192.168.1.1",
        ["Dialog_Add"] = "追加",
        ["Dialog_Cancel"] = "キャンセル",
        ["Dialog_Close"] = "閉じる",

        // 設定
        ["Settings_Title"] = "アプリケーション設定",
        ["Settings_Theme"] = "テーマ設定",
        ["Settings_Theme_System"] = "Windowsのシステム設定に従う",
        ["Settings_Theme_Light"] = "ライト",
        ["Settings_Theme_Dark"] = "ダーク",
        ["Settings_Language"] = "表示言語",
        ["Settings_Language_Auto"] = "システム設定に従う (自動)",
        ["Settings_Language_Ja"] = "日本語 (Japanese)",
        ["Settings_Language_En"] = "英語 (English)",
        ["Settings_SavePath"] = "設定保存先:",

        // ステータス表示
        ["Status_Idle"] = "待機中",
        ["Status_Running"] = "Ping 送信中...",
        ["Status_Success"] = "応答あり",
        ["Status_Timeout"] = "タイムアウト",
        ["Status_Unreachable"] = "到達不能",
        ["Status_Error"] = "エラー",

        // ドーナツグラフ
        ["Toolbar_ToggleChart"] = "グラフ",
        ["Chart_Title"] = "Ping 状況",
        ["Chart_Overall"] = "全体",
        ["Chart_SelectedHost"] = "選択中のホスト",
        ["Chart_NoHostSelected"] = "一覧からホストを選択すると\n個別の状況を表示します",
        ["Chart_OkLabel"] = "OK (成功)",
        ["Chart_NgLabel"] = "NG (失敗)",
        ["Chart_TotalFormat"] = "合計: {0}"
    };

    private readonly Dictionary<string, string> _enStrings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Title & Header
        ["App_Title"] = "WoodStream Pinger",
        ["App_Subtitle"] = "Multi-Host Concurrent Ping & Real-Time Network Statistics Tool",
        ["App_Version"] = "Version 1.0.1.0",

        // Toolbar
        ["Toolbar_StartAll"] = "Start All",
        ["Toolbar_StopAll"] = "Stop All",
        ["Toolbar_ResetStats"] = "Reset Stats",
        ["Toolbar_AddHost"] = "Add Hosts",
        ["Toolbar_ExportCsv"] = "Export Results (CSV)",
        ["Toolbar_Settings"] = "Settings",
        ["Toolbar_SearchPlaceholder"] = "Filter by host or IP...",

        // CSV Export
        ["Export_Success_Title"] = "Export Completed",
        ["Export_Success_Message"] = "Statistics exported to CSV file:\n{0}",
        ["Export_NoData"] = "No hosts to export.",

        // Ping Execution Log (CSV)
        ["Log_CsvHeader"] = "Timestamp,Destination,Response Time (ms),TTL",
        ["Log_Enable"] = "Record Ping Log (CSV)",
        ["Log_Folder"] = "Log Folder:",
        ["Log_Browse"] = "Browse...",
        ["Log_OpenFolder"] = "Open Folder",
        ["Log_RecordingBadge"] = "Logging",

        // Parameters
        ["Param_Title"] = "Ping Parameters",
        ["Param_Timeout"] = "Timeout (ms):",
        ["Param_Interval"] = "Interval (ms):",
        ["Param_BufferSize"] = "Buffer Size (Bytes):",
        ["Param_Ttl"] = "TTL (Time To Live):",
        ["Param_DontFragment"] = "Don't Fragment (DF)",
        ["Param_Continuous"] = "Continuous loop until stopped",
        ["Param_Count"] = "Attempt Count:",

        // Grid & List Columns
        ["Grid_Host"] = "Host / IP",
        ["Grid_ResolvedIp"] = "Resolved IP",
        ["Grid_Status"] = "Status",
        ["Grid_LastRtt"] = "Last RTT",
        ["Grid_Sent"] = "Sent",
        ["Grid_Received"] = "Received",
        ["Grid_Lost"] = "Lost",
        ["Grid_LossRate"] = "Loss %",
        ["Grid_MinRtt"] = "Min RTT",
        ["Grid_MaxRtt"] = "Max RTT",
        ["Grid_AvgRtt"] = "Avg RTT",
        ["Grid_Actions"] = "Actions",
        ["Grid_Start"] = "Start",
        ["Grid_Stop"] = "Stop",
        ["Grid_Delete"] = "Remove",
        ["Grid_Reset"] = "Reset",

        // Summary Status Bar
        ["Status_TotalHosts"] = "Total Hosts:",
        ["Status_ActiveHosts"] = "Active:",
        ["Status_TotalSent"] = "Total Sent:",
        ["Status_TotalReceived"] = "Total Received:",
        ["Status_TotalLost"] = "Total Lost:",
        ["Status_OverallLossRate"] = "Loss Rate:",
        ["Status_OverallAvgRtt"] = "Overall Avg RTT:",

        // Dialog & Settings
        ["Dialog_AddHost_Title"] = "Add Hosts",
        ["Dialog_AddHost_Description"] = "Enter host names or IP addresses (multi-line or comma separated):",
        ["Dialog_AddHost_Placeholder"] = "e.g.:\n8.8.8.8\n1.1.1.1\ngoogle.com\n192.168.1.1",
        ["Dialog_Add"] = "Add",
        ["Dialog_Cancel"] = "Cancel",
        ["Dialog_Close"] = "Close",

        // Settings
        ["Settings_Title"] = "Application Settings",
        ["Settings_Theme"] = "Appearance Theme",
        ["Settings_Theme_System"] = "Follow Windows system setting",
        ["Settings_Theme_Light"] = "Light",
        ["Settings_Theme_Dark"] = "Dark",
        ["Settings_Language"] = "Display Language",
        ["Settings_Language_Auto"] = "Follow system language (Auto)",
        ["Settings_Language_Ja"] = "Japanese (日本語)",
        ["Settings_Language_En"] = "English",
        ["Settings_SavePath"] = "Settings File Path:",

        // Status
        ["Status_Idle"] = "Idle",
        ["Status_Running"] = "Pinging...",
        ["Status_Success"] = "Reply Received",
        ["Status_Timeout"] = "Timed Out",
        ["Status_Unreachable"] = "Unreachable",
        ["Status_Error"] = "Error",

        // Donut Chart
        ["Toolbar_ToggleChart"] = "Chart",
        ["Chart_Title"] = "Ping Status",
        ["Chart_Overall"] = "Overall",
        ["Chart_SelectedHost"] = "Selected Host",
        ["Chart_NoHostSelected"] = "Select a host from the list\nto view its status",
        ["Chart_OkLabel"] = "OK (Success)",
        ["Chart_NgLabel"] = "NG (Failed)",
        ["Chart_TotalFormat"] = "Total: {0}"
    };

    public LocalizationService()
    {
        // OS の表示言語を判別（デフォルト）
        var currentUiCulture = CultureInfo.CurrentUICulture.Name;
        _currentLanguage = currentUiCulture.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
            ? "ja-JP"
            : "en-US";
    }

    public string GetString(string key)
    {
        var dict = _currentLanguage.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
            ? _jaStrings
            : _enStrings;

        if (dict.TryGetValue(key, out var val))
        {
            return val;
        }

        // 英語辞書フォールバック
        if (_enStrings.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public void SetLanguage(string languageCode)
    {
        if (string.Equals(languageCode, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            var uiLang = CultureInfo.CurrentUICulture.Name;
            CurrentLanguage = uiLang.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "ja-JP" : "en-US";
        }
        else if (languageCode.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
        {
            CurrentLanguage = "ja-JP";
        }
        else
        {
            CurrentLanguage = "en-US";
        }
    }
}
