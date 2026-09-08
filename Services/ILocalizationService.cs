using System.ComponentModel;
using System.Globalization;

namespace WoodStreamPinger.Services;

/// <summary>
/// 多言語化対応サービスインターフェース
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; set; }
    string this[string key] { get; }
    string GetString(string key);
    void SetLanguage(string languageCode);
}
