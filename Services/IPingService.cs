using WoodStreamPinger.Models;

namespace WoodStreamPinger.Services;

/// <summary>
/// Ping 送信および非同期監視サービスクラスのインターフェース
/// </summary>
public interface IPingService
{
    Task<PingResultItem> SendSinglePingAsync(
        string host,
        PingParameters parameters,
        CancellationToken cancellationToken = default);

    Task RunPingLoopAsync(
        HostPingTarget target,
        PingParameters parameters,
        Action<HostPingTarget, PingResultItem, string?> onResultUpdated,
        CancellationToken cancellationToken);
}
