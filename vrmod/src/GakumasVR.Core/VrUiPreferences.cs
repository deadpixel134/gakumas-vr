using System.Globalization;

namespace GakumasVR.Core;

public static class VrUiPreferences
{
    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GakumasVR");

    public static string Code(VrUiLanguage language) => language switch
    { VrUiLanguage.English => "en", VrUiLanguage.Japanese => "ja", _ => "ko" };

    public static VrUiLanguage Parse(string code) => code.Trim().ToLowerInvariant() switch
    { "en" => VrUiLanguage.English, "ja" => VrUiLanguage.Japanese, _ => VrUiLanguage.Korean };

    public static VrUiLanguage Load(string? directory = null, CultureInfo? culture = null)
    {
        try
        {
            string root = directory ?? DirectoryPath;
            string path = Path.Combine(root, "ui-language.txt");
            if (!File.Exists(path)) path = Path.Combine(root, "configurator-language.txt");
            if (File.Exists(path)) return Parse(File.ReadAllText(path));
        }
        catch { /* A preference read must not stop either UI. */ }
        return (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName switch
        { "en" => VrUiLanguage.English, "ja" => VrUiLanguage.Japanese, _ => VrUiLanguage.Korean };
    }

    public static bool TrySave(VrUiLanguage language, string? directory = null)
    {
        string? temporary = null;
        try
        {
            if (!Enum.IsDefined(typeof(VrUiLanguage), language)) return false;
            string root = directory ?? DirectoryPath;
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "ui-language.txt");
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, Code(language));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
            return true;
        }
        catch { return false; }
        finally { try { if (temporary is not null && File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }
}
