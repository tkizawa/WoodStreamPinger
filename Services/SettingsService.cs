using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using WoodStreamPinger.Models;

namespace WoodStreamPinger.Services;

/// <summary>
/// アプリケーション設定を AppData\Local\WoodStreamPinger に保存・管理するサービス
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WoodStreamPinger");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        // 設定ファイル内の日本語をUnicodeエスケープせず、UTF-8可視テキストとして保持
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppSettings CurrentSettings { get; private set; } = new();

    /// <summary>
    /// 設定ファイルから読み込み、存在しない場合はデフォルト設定を生成
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (loaded != null)
                {
                    CurrentSettings = loaded;
                    return CurrentSettings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] 設定読み込みエラー: {ex.Message}");
        }

        CurrentSettings = new AppSettings();
        return CurrentSettings;
    }

    /// <summary>
    /// 設定ファイルを保存（日本語はUnicodeエスケープせずUTF-8可視テキストで保存）
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings? settings = null)
    {
        try
        {
            if (settings != null)
            {
                CurrentSettings = settings;
            }

            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var json = JsonSerializer.Serialize(CurrentSettings, _jsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] 設定保存エラー: {ex.Message}");
        }
    }
}
