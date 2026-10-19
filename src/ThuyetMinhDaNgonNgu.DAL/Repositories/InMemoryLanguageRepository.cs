using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

/// <summary>
/// Danh sách ngôn ngữ mẫu. Có thể thay bằng bảng Language trong SQLite ở giai đoạn sau.
/// </summary>
public sealed class InMemoryLanguageRepository : ILanguageRepository
{
    private static readonly List<SupportedLanguage> Languages =
    [
        new SupportedLanguage("vi", "Tiếng Việt"),
        new SupportedLanguage("en", "English"),
        new SupportedLanguage("ja", "日本語"),
        new SupportedLanguage("ko", "한국어"),
        new SupportedLanguage("fr", "Français"),
        new SupportedLanguage("zh", "中文")
    ];

    public IReadOnlyList<SupportedLanguage> GetAll()
    {
        lock (Languages)
        {
            return Languages.OrderBy(language => language.Code).ToArray();
        }
    }

    public SupportedLanguage? GetByCode(string code)
    {
        lock (Languages)
        {
            return Languages.FirstOrDefault(language =>
                string.Equals(language.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool TryAdd(SupportedLanguage language)
    {
        lock (Languages)
        {
            if (Languages.Any(item =>
                    string.Equals(item.Code, language.Code, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            Languages.Add(language);
            return true;
        }
    }
}
