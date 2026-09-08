using WoodStreamPinger.Models;

namespace WoodStreamPinger.Services;

/// <summary>
/// アプリケーション設定の読み込み・保存を行うサービスインターフェース
/// </summary>
public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings? settings = null);
}
