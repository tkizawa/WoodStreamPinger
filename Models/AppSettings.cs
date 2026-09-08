namespace WoodStreamPinger.Models;

/// <summary>
/// アプリケーション設定保存用モデル
/// </summary>
public sealed class AppSettings
{
    // ウィンドウ状態
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowWidth { get; set; } = 1100;
    public int WindowHeight { get; set; } = 720;
    public bool IsMaximized { get; set; }

    // Ping パラメータ設定
    public PingParameters Parameters { get; set; } = new();

    // 登録ホストリスト
    public List<string> SavedHosts { get; set; } =
    [
        "8.8.8.8",
        "1.1.1.1",
        "google.com",
        "127.0.0.1"
    ];

    // テーマ設定 ("System", "Light", "Dark")
    public string Theme { get; set; } = "System";

    // 言語設定 ("Auto", "ja-JP", "en-US")
    public string Language { get; set; } = "Auto";
}
