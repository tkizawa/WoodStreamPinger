using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using WoodStreamPinger.Models;
using WoodStreamPinger.Services;

namespace WoodStreamPinger.ViewModels;

/// <summary>
/// メインウィンドウ・画面全体の ViewModel
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IPingService _pingService;
    private readonly IPingLogService _pingLogService;
    private readonly ISettingsService _settingsService;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly DispatcherQueue _dispatcherQueue;

    private readonly ConcurrentDictionary<HostPingTarget, CancellationTokenSource> _runningTasks = new();
    private bool _isInitializing;
    private bool _isDisposed;

    // --- 公開プロパティ ---

    public LocalizationService Loc => _loc;

    [ObservableProperty]
    public partial PingParameters Parameters { get; set; } = new();

    /// <summary>
    /// 登録されている全ホスト
    /// </summary>
    public ObservableCollection<HostPingTarget> Targets { get; } = [];

    [ObservableProperty]
    public partial HostPingTarget? SelectedTarget { get; set; }

    /// <summary>
    /// ドーナツ円グラフパネルの表示状態
    /// </summary>
    [ObservableProperty]
    public partial bool IsChartPanelVisible { get; set; } = true;

    /// <summary>
    /// ホストが選択されているかどうか
    /// </summary>
    public bool HasSelectedTarget => SelectedTarget != null;

    /// <summary>
    /// ホストが未選択かどうか
    /// </summary>
    public bool HasNoSelectedTarget => SelectedTarget == null;

    partial void OnSelectedTargetChanged(HostPingTarget? value)
    {
        OnPropertyChanged(nameof(HasSelectedTarget));
        OnPropertyChanged(nameof(HasNoSelectedTarget));
    }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAnyRunning { get; set; }

    [ObservableProperty]
    public partial string NewHostInput { get; set; } = string.Empty;

    // --- Ping 実行ログ (CSV) 設定プロパティ ---

    [ObservableProperty]
    public partial bool EnablePingLogging { get; set; }

    [ObservableProperty]
    public partial string LogDirectory { get; set; } = string.Empty;

    partial void OnEnablePingLoggingChanged(bool value)
    {
        if (_isInitializing) return;
        SaveLogSettings();
    }

    partial void OnLogDirectoryChanged(string value)
    {
        if (_isInitializing) return;
        SaveLogSettings();
    }

    /// <summary>
    /// 実際に使用されるログ保存先フォルダパスを取得
    /// </summary>
    public string GetResolvedLogDirectory() => _pingLogService.ResolveLogDirectory(LogDirectory);

    // --- サマリー統計プロパティ ---

    [ObservableProperty]
    public partial int TotalHostsCount { get; set; }

    [ObservableProperty]
    public partial int ActiveHostsCount { get; set; }

    [ObservableProperty]
    public partial int TotalSent { get; set; }

    [ObservableProperty]
    public partial int TotalReceived { get; set; }

    [ObservableProperty]
    public partial int TotalLost { get; set; }

    [ObservableProperty]
    public partial double OverallLossRate { get; set; }

    [ObservableProperty]
    public partial double OverallAvgRtt { get; set; }

    // --- 表示用フォーマット済みプロパティ ---
    public string OverallLossRateDisplay => $"{OverallLossRate}%";
    public string OverallAvgRttDisplay => $"{OverallAvgRtt} ms";

    // --- NumberBox バインディング用 double プロパティ ---
    public double TimeoutMs
    {
        get => Parameters.TimeoutMs;
        set { Parameters.TimeoutMs = (int)Math.Round(value); OnPropertyChanged(); }
    }
    public double IntervalMs
    {
        get => Parameters.IntervalMs;
        set { Parameters.IntervalMs = (int)Math.Round(value); OnPropertyChanged(); }
    }
    public double BufferSize
    {
        get => Parameters.BufferSize;
        set { Parameters.BufferSize = (int)Math.Round(value); OnPropertyChanged(); }
    }
    public double Ttl
    {
        get => Parameters.Ttl;
        set { Parameters.Ttl = (int)Math.Round(value); OnPropertyChanged(); }
    }
    public double Count
    {
        get => Parameters.Count;
        set { Parameters.Count = (int)Math.Round(value); OnPropertyChanged(); }
    }

    public MainViewModel(
        IPingService pingService,
        IPingLogService pingLogService,
        ISettingsService settingsService,
        DispatcherQueue dispatcherQueue)
    {
        _pingService = pingService;
        _pingLogService = pingLogService;
        _settingsService = settingsService;
        _dispatcherQueue = dispatcherQueue;

        // 設定の初期ロード
        LoadInitialSettings();
    }

    private void LoadInitialSettings()
    {
        _isInitializing = true;
        try
        {
            var settings = _settingsService.CurrentSettings;
            Parameters = settings.Parameters.Clone();
            EnablePingLogging = settings.EnablePingLogging;
            LogDirectory = _pingLogService.ResolveLogDirectory(settings.LogDirectory);

            Targets.Clear();
            foreach (var host in settings.SavedHosts)
            {
                if (!string.IsNullOrWhiteSpace(host))
                {
                    Targets.Add(new HostPingTarget(host));
                }
            }

            UpdateSummaryStats();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    // --- コマンド実装 ---

    /// <summary>
    /// 全ホストに対する Ping を開始
    /// </summary>
    [RelayCommand]
    public void StartAll()
    {
        foreach (var target in Targets.Where(t => t.IsEnabled))
        {
            StartPingForTarget(target);
        }
        UpdateRunningState();
    }

    /// <summary>
    /// 全ホストに対する Ping を停止
    /// </summary>
    [RelayCommand]
    public void StopAll()
    {
        foreach (var kvp in _runningTasks)
        {
            try
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            catch { }
        }
        _runningTasks.Clear();

        foreach (var target in Targets)
        {
            if (target.Status == HostPingStatus.Running)
            {
                target.Status = HostPingStatus.Idle;
                target.StatusText = "Stopped";
            }
        }

        UpdateRunningState();
    }

    /// <summary>
    /// 個別ホストに対する Ping 開始
    /// </summary>
    [RelayCommand]
    public void StartTarget(HostPingTarget? target)
    {
        if (target == null) return;
        StartPingForTarget(target);
        UpdateRunningState();
    }

    /// <summary>
    /// 個別ホストに対する Ping 停止
    /// </summary>
    [RelayCommand]
    public void StopTarget(HostPingTarget? target)
    {
        if (target == null) return;
        if (_runningTasks.TryRemove(target, out var cts))
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch { }
        }

        if (target.Status == HostPingStatus.Running)
        {
            target.Status = HostPingStatus.Idle;
            target.StatusText = "Stopped";
        }
        UpdateRunningState();
    }

    /// <summary>
    /// 入力されたホスト（改行またはカンマ区切りで複数可）を追加
    /// </summary>
    [RelayCommand]
    public void AddHosts(string? input)
    {
        var text = string.IsNullOrWhiteSpace(input) ? NewHostInput : input;
        if (string.IsNullOrWhiteSpace(text)) return;

        var entries = text.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim())
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .Distinct(StringComparer.OrdinalIgnoreCase);

        bool added = false;
        foreach (var entry in entries)
        {
            if (!Targets.Any(t => string.Equals(t.Host, entry, StringComparison.OrdinalIgnoreCase)))
            {
                Targets.Add(new HostPingTarget(entry));
                added = true;
            }
        }

        if (added)
        {
            NewHostInput = string.Empty;
            UpdateSummaryStats();
            SaveHostsToSettings();
        }
    }

    /// <summary>
    /// ホストをリストから削除
    /// </summary>
    [RelayCommand]
    public void RemoveTarget(HostPingTarget? target)
    {
        if (target == null) return;

        StopTarget(target);
        Targets.Remove(target);
        UpdateSummaryStats();
        SaveHostsToSettings();
    }

    /// <summary>
    /// 全ホストの統計をリセット
    /// </summary>
    [RelayCommand]
    public void ResetStats()
    {
        foreach (var target in Targets)
        {
            target.ResetStats();
        }
        UpdateSummaryStats();
    }

    /// <summary>
    /// 全ホストリストをクリア
    /// </summary>
    [RelayCommand]
    public void ClearAllTargets()
    {
        StopAll();
        Targets.Clear();
        UpdateSummaryStats();
        SaveHostsToSettings();
    }

    /// <summary>
    /// 現在の全ホスト統計結果を CSV ファイルとして出力（BOM付きUTF-8）
    /// </summary>
    public async Task ExportCsvAsync(string filePath)
    {
        var sb = new System.Text.StringBuilder();
        // CSV ヘッダー行
        sb.AppendLine("Host,Resolved IP,Status,Last RTT (ms),Sent,Received,Lost,Loss Rate (%),Min RTT (ms),Max RTT (ms),Avg RTT (ms)");

        foreach (var t in Targets)
        {
            var hostEscaped = $"\"{t.Host.Replace("\"", "\"\"")}\"";
            var ip = $"\"{t.ResolvedIp.Replace("\"", "\"\"")}\"";
            var status = $"\"{t.StatusText.Replace("\"", "\"\"")}\"";
            var lastRtt = t.Status == HostPingStatus.Success ? t.LastRttMs.ToString() : "-";
            var minRtt = t.MinRttMs >= 0 ? t.MinRttMs.ToString() : "-";
            var maxRtt = t.MaxRttMs >= 0 ? t.MaxRttMs.ToString() : "-";
            var avgRtt = t.ReceivedCount > 0 ? t.AvgRttMs.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";

            sb.AppendLine($"{hostEscaped},{ip},{status},{lastRtt},{t.SentCount},{t.ReceivedCount},{t.LostCount},{t.LossRate}%,{minRtt},{maxRtt},{avgRtt}");
        }

        // Excel等での文字化けを防ぐため BOM 付き UTF-8 で出力
        var encoding = new System.Text.UTF8Encoding(true);
        await File.WriteAllTextAsync(filePath, sb.ToString(), encoding);
    }

    // --- 内部ヘルパーロジック ---

    private void StartPingForTarget(HostPingTarget target)
    {
        // 既に実行中なら停止してから再開
        if (_runningTasks.TryRemove(target, out var oldCts))
        {
            try
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            catch { }
        }

        var cts = new CancellationTokenSource();
        _runningTasks[target] = cts;
        target.SetRunning();

        var paramCopy = Parameters.Clone();

        _ = Task.Run(async () =>
        {
            try
            {
                await _pingService.RunPingLoopAsync(
                    target,
                    paramCopy,
                    (t, result, resolvedIp) =>
                    {
                        // Ping 実行ログ記録が有効な場合、バックグラウンドスレッドで CSV に 1 行追記
                        if (EnablePingLogging)
                        {
                            _pingLogService.WriteEntry(
                                t.Host,
                                result,
                                LogDirectory,
                                _loc.GetString("Log_CsvHeader"));
                        }

                        // UI スレッド上でモデル・プロパティを更新
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            t.UpdateWithResult(result, resolvedIp);
                            UpdateSummaryStats();
                        });
                    },
                    cts.Token);
            }
            finally
            {
                _runningTasks.TryRemove(target, out _);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (target.Status == HostPingStatus.Running)
                    {
                        target.Status = HostPingStatus.Idle;
                        target.StatusText = "Finished";
                    }
                    UpdateRunningState();
                    UpdateSummaryStats();
                });
            }
        }, cts.Token);
    }

    private void UpdateRunningState()
    {
        IsAnyRunning = !_runningTasks.IsEmpty;
        ActiveHostsCount = _runningTasks.Count;
    }

    private void UpdateSummaryStats()
    {
        TotalHostsCount = Targets.Count;
        ActiveHostsCount = _runningTasks.Count;

        int sent = 0;
        int received = 0;
        int lost = 0;
        double sumAvgRtt = 0;
        int hostsWithReplies = 0;

        foreach (var t in Targets)
        {
            sent += t.SentCount;
            received += t.ReceivedCount;
            lost += t.LostCount;

            if (t.ReceivedCount > 0)
            {
                sumAvgRtt += t.AvgRttMs;
                hostsWithReplies++;
            }
        }

        TotalSent = sent;
        TotalReceived = received;
        TotalLost = lost;

        OverallLossRate = sent > 0
            ? Math.Round((double)lost / sent * 100.0, 1)
            : 0.0;

        OverallAvgRtt = hostsWithReplies > 0
            ? Math.Round(sumAvgRtt / hostsWithReplies, 1)
            : 0.0;

        OnPropertyChanged(nameof(OverallLossRateDisplay));
        OnPropertyChanged(nameof(OverallAvgRttDisplay));
    }

    private void SaveHostsToSettings()
    {
        var settings = _settingsService.CurrentSettings;
        settings.SavedHosts = Targets.Select(t => t.Host).ToList();
        settings.Parameters = Parameters.Clone();
        settings.EnablePingLogging = EnablePingLogging;
        settings.LogDirectory = LogDirectory;
        _ = _settingsService.SaveSettingsAsync(settings);
    }

    private void SaveLogSettings()
    {
        var settings = _settingsService.CurrentSettings;
        settings.EnablePingLogging = EnablePingLogging;
        settings.LogDirectory = LogDirectory;
        _ = _settingsService.SaveSettingsAsync(settings);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        StopAll();
        SaveHostsToSettings();
    }
}
