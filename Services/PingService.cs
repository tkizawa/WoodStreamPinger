using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WoodStreamPinger.Models;

namespace WoodStreamPinger.Services;

/// <summary>
/// 各種ネットワークパラメータを適用して Ping を非同期に送信・計測するサービス
/// </summary>
public sealed class PingService : IPingService
{
    private static readonly Random Rnd = new();

    /// <summary>
    /// 1 回の Ping 送信を実行
    /// </summary>
    public async Task<PingResultItem> SendSinglePingAsync(
        string host,
        PingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return new PingResultItem
            {
                Status = HostPingStatus.Error,
                Message = "Host is empty"
            };
        }

        byte[] buffer = new byte[Math.Clamp(parameters.BufferSize, 1, 65500)];
        Rnd.NextBytes(buffer);

        var options = new PingOptions(
            ttl: Math.Clamp(parameters.Ttl, 1, 255),
            dontFragment: parameters.DontFragment);

        using var pinger = new Ping();

        try
        {
            // 非同期Ping送信 (タイムアウトおよびキャンセルトークン考慮)
            var reply = await pinger.SendPingAsync(
                hostNameOrAddress: host.Trim(),
                timeout: Math.Max(100, parameters.TimeoutMs),
                buffer: buffer,
                options: options).WaitAsync(cancellationToken);

            if (reply.Status == IPStatus.Success)
            {
                return new PingResultItem
                {
                    Status = HostPingStatus.Success,
                    RoundtripTimeMs = reply.RoundtripTime,
                    Ttl = reply.Options?.Ttl ?? parameters.Ttl,
                    BufferSize = reply.Buffer?.Length ?? buffer.Length,
                    Message = "Reply from " + (reply.Address?.ToString() ?? host)
                };
            }
            else if (reply.Status == IPStatus.TimedOut)
            {
                return new PingResultItem
                {
                    Status = HostPingStatus.Timeout,
                    RoundtripTimeMs = parameters.TimeoutMs,
                    Message = "Request timed out"
                };
            }
            else if (reply.Status == IPStatus.DestinationHostUnreachable ||
                     reply.Status == IPStatus.DestinationNetworkUnreachable ||
                     reply.Status == IPStatus.DestinationUnreachable)
            {
                return new PingResultItem
                {
                    Status = HostPingStatus.DestinationUnreachable,
                    Message = $"Destination unreachable ({reply.Status})"
                };
            }
            else
            {
                return new PingResultItem
                {
                    Status = HostPingStatus.Error,
                    Message = reply.Status.ToString()
                };
            }
        }
        catch (OperationCanceledException)
        {
            return new PingResultItem
            {
                Status = HostPingStatus.Idle,
                Message = "Canceled"
            };
        }
        catch (PingException ex)
        {
            return new PingResultItem
            {
                Status = HostPingStatus.Error,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
        catch (SocketException ex)
        {
            return new PingResultItem
            {
                Status = HostPingStatus.Error,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new PingResultItem
            {
                Status = HostPingStatus.Error,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 指定ホストに対する Ping ループを非同期に実行
    /// </summary>
    public async Task RunPingLoopAsync(
        HostPingTarget target,
        PingParameters parameters,
        Action<HostPingTarget, PingResultItem, string?> onResultUpdated,
        CancellationToken cancellationToken)
    {
        string? resolvedIp = null;

        // 初回名前解決（可能であれば）
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(target.Host, cancellationToken);
            if (addresses.Length > 0)
            {
                resolvedIp = addresses[0].ToString();
            }
        }
        catch
        {
            // 名前解決失敗はPing自体のエラーで報告
        }

        int attempts = 0;
        int maxAttempts = parameters.Count; // 0 は無制限

        while (!cancellationToken.IsCancellationRequested)
        {
            attempts++;

            var stopwatch = Stopwatch.StartNew();
            var result = await SendSinglePingAsync(target.Host, parameters, cancellationToken);
            stopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // UI更新コールバック
            onResultUpdated(target, result, resolvedIp);

            // 試行回数上限チェック
            if (maxAttempts > 0 && attempts >= maxAttempts)
            {
                break;
            }

            // 次の送信インターバルまで待機（計測にかかった時間を考慮しインターバルを調整）
            var remainingDelay = parameters.IntervalMs - (int)stopwatch.ElapsedMilliseconds;
            if (remainingDelay < 50)
            {
                remainingDelay = 50; // 最低 50ms の間隔を設ける
            }

            try
            {
                await Task.Delay(remainingDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
