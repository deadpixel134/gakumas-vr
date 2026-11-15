using System.Net;
using System.Text.RegularExpressions;
using GakumasVR.Core;

namespace GakumasVR.Management;

// A bounded, plain-text excerpt of the publisher's notes, never executable Markdown/HTML.
public static class ReleaseNotesSummary
{
    public const int MaximumItems = 5;
    public const int MaximumItemLength = 220;
    private const int MaximumSourceLength = 65536;

    public static string Create(string? markdown, VrUiLanguage language)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        string source = markdown[..Math.Min(markdown.Length, MaximumSourceLength)];
        source = Regex.Replace(source, @"<!--[\s\S]*?(?:-->|\z)", "");
        string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        Dictionary<VrUiLanguage, List<string>> sections = new();
        VrUiLanguage? current = null;
        bool fenced = false;
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (IsFence(trimmed)) { fenced = !fenced; continue; }
            if (fenced) continue;
            if (LanguageHeading(trimmed) is VrUiLanguage heading)
            {
                current = heading;
                sections.TryAdd(heading, new());
            }
            else if (current is VrUiLanguage active) sections[active].Add(line);
        }

        IEnumerable<string> selected = lines;
        if (sections.Count > 0)
        {
            selected = sections.TryGetValue(language, out List<string>? preferred) && HasContent(preferred)
                ? preferred
                : sections.TryGetValue(VrUiLanguage.English, out List<string>? english) && HasContent(english)
                    ? english
                    : sections.Values.FirstOrDefault(HasContent) ?? new List<string>();
        }

        List<string> items = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        fenced = false;
        foreach (string line in selected)
        {
            string trimmed = line.Trim();
            if (IsFence(trimmed)) { fenced = !fenced; continue; }
            if (fenced) continue;
            string plain = PlainText(trimmed);
            if (plain.Length == 0 || !seen.Add(plain)) continue;
            if (plain.Length > MaximumItemLength)
            {
                int count = MaximumItemLength - 1;
                if (char.IsHighSurrogate(plain[count - 1])) count--;
                plain = plain[..count].TrimEnd() + "…";
            }
            items.Add("• " + plain);
            if (items.Count == MaximumItems) break;
        }
        return string.Join(Environment.NewLine + Environment.NewLine, items);
    }

    private static bool HasContent(List<string> lines) => lines.Any(line => PlainText(line.Trim()).Length > 0);

    private static bool IsFence(string line) => line.StartsWith("```") || line.StartsWith("~~~");

    private static VrUiLanguage? LanguageHeading(string line)
    {
        if (!Regex.IsMatch(line, @"^#{1,6}\s")) return null;
        if (line.Contains("한국어") || Regex.IsMatch(line, @"\bKorean\b", RegexOptions.IgnoreCase)) return VrUiLanguage.Korean;
        if (line.Contains("日本語") || Regex.IsMatch(line, @"\bJapanese\b", RegexOptions.IgnoreCase)) return VrUiLanguage.Japanese;
        if (Regex.IsMatch(line, @"\bEnglish\b", RegexOptions.IgnoreCase)) return VrUiLanguage.English;
        return null;
    }

    private static string PlainText(string line)
    {
        if (line.Length == 0 || line.StartsWith('#') || IsFence(line) ||
            Regex.IsMatch(line, @"^(?:[-*_]\s*){3,}$|^\[[^\]]+\]:|^\|?[\s:|\-]+\|$")) return string.Empty;
        line = Regex.Replace(line, @"!\[[^\]]*\]\([^)]*\)", "");
        line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]*\)", "$1");
        line = Regex.Replace(line, @"<[^>]*>", "");
        line = WebUtility.HtmlDecode(line);
        line = Regex.Replace(line, @"^\s*(?:[-+*•>]\s+|\d+[.)]\s+|\[[ xX]\]\s*)+", "");
        line = line.Replace("**", "").Replace("__", "").Replace("~~", "").Replace("`", "");
        line = Regex.Replace(line, @"[\p{Cc}\p{Cf}]", " ");
        return Regex.Replace(line, @"\s+", " ").Trim();
    }
}
