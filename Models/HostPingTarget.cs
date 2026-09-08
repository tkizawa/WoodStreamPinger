using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WoodStreamPinger.Models;

/// <summary>
/// Ping 対象ホストおよびそのリアルタイム統計情報を保持するモデル
/// </summary>
public sealed partial class HostPingTarget : ObservableObject
{
    private readonly object _lock = new();
    private long _totalRttSum;

    [ObservableProperty]
    public partial string Host { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResolvedIp { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial HostPingStatus Status { get; set; } = HostPingStatus.Idle;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Idle";

    [ObservableProperty]
    public partial long LastRttMs { get; set; }

    [ObservableProperty]
    public partial int SentCount { get; set; }

    [ObservableProperty]
    public partial int ReceivedCount { get; set; }

    [ObservableProperty]
    public partial int LostCount { get; set; }

    [ObservableProperty]
    public partial double LossRate { get; set; }

    [ObservableProperty]
    public partial long MinRttMs { get; set; } = -1;

    [ObservableProperty]
    public partial long MaxRttMs { get; set; } = -1;

    [ObservableProperty]
    public partial double AvgRttMs { get; set; }

    [ObservableProperty]
    public partial string LastErrorMessage { get; set; } = string.Empty;

    // --- 表示用フォーマット済みプロパティ ---
    public string LossDisplay => $"{LostCount} ({LossRate}%)";
    public string MinRttDisplay => MinRttMs >= 0 ? $"{MinRttMs} ms" : "-";
    public string MaxRttDisplay => MaxRttMs >= 0 ? $"{MaxRttMs} ms" : "-";
    public string AvgRttDisplay => ReceivedCount > 0 ? $"{AvgRttMs} ms" : "-";
    public string LastRttDisplay => Status == HostPingStatus.Success ? $"{LastRttMs} ms" : "-";

    /// <summary>
    /// 直近の Ping 結果履歴（最新 20 件を保持）
    /// </summary>
    public ObservableCollection<PingResultItem> RecentHistory { get; } = [];

    public HostPingTarget(string host)
    {
        Host = host.Trim();
        DisplayName = Host;
    }

    /// <summary>
    /// Ping 実行開始時のステータス更新
    /// </summary>
    public void SetRunning()
    {
        Status = HostPingStatus.Running;
        StatusText = "Pinging...";
    }

    /// <summary>
    /// Ping 結果を反映して統計を更新
    /// </summary>
    public void UpdateWithResult(PingResultItem result, string? resolvedIp = null)
    {
        lock (_lock)
        {
            SentCount++;
            Status = result.Status;
            StatusText = result.Status switch
            {
                HostPingStatus.Success => $"{result.RoundtripTimeMs} ms",
                HostPingStatus.Timeout => "Request timed out",
                HostPingStatus.DestinationUnreachable => "Destination unreachable",
                HostPingStatus.Error => result.Message ?? "Error",
                _ => result.Status.ToString()
            };

            if (!string.IsNullOrEmpty(resolvedIp))
            {
                ResolvedIp = resolvedIp;
            }

            if (result.Status == HostPingStatus.Success)
            {
                ReceivedCount++;
                LastRttMs = result.RoundtripTimeMs;
                _totalRttSum += result.RoundtripTimeMs;

                if (MinRttMs < 0 || result.RoundtripTimeMs < MinRttMs)
                {
                    MinRttMs = result.RoundtripTimeMs;
                }

                if (result.RoundtripTimeMs > MaxRttMs)
                {
                    MaxRttMs = result.RoundtripTimeMs;
                }

                AvgRttMs = Math.Round((double)_totalRttSum / ReceivedCount, 1);
                LastErrorMessage = string.Empty;
            }
            else
            {
                LostCount++;
                LastErrorMessage = result.Message ?? StatusText;
            }

            LossRate = SentCount > 0
                ? Math.Round((double)LostCount / SentCount * 100.0, 1)
                : 0.0;

            // 履歴リストの追加（最大20件）
            RecentHistory.Insert(0, result);
            while (RecentHistory.Count > 20)
            {
                RecentHistory.RemoveAt(RecentHistory.Count - 1);
            }

            NotifyDisplayProperties();
        }
    }

    /// <summary>
    /// 統計データをゼロにリセット
    /// </summary>
    public void ResetStats()
    {
        lock (_lock)
        {
            SentCount = 0;
            ReceivedCount = 0;
            LostCount = 0;
            LossRate = 0;
            MinRttMs = -1;
            MaxRttMs = -1;
            AvgRttMs = 0;
            LastRttMs = 0;
            _totalRttSum = 0;
            Status = HostPingStatus.Idle;
            StatusText = "Idle";
            LastErrorMessage = string.Empty;
            RecentHistory.Clear();

            NotifyDisplayProperties();
        }
    }

    private void NotifyDisplayProperties()
    {
        OnPropertyChanged(nameof(LossDisplay));
        OnPropertyChanged(nameof(MinRttDisplay));
        OnPropertyChanged(nameof(MaxRttDisplay));
        OnPropertyChanged(nameof(AvgRttDisplay));
        OnPropertyChanged(nameof(LastRttDisplay));
    }
}
