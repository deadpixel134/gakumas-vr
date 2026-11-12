namespace GakumasVR.Core;

public static class ContactConsent
{
    private const string AcceptedNotice = "character-contact-v2";
    private const string FileName = "character-contact-consent.txt";

    public static bool IsAccepted(string? directory = null)
    {
        try
        {
            string path = Path.Combine(directory ?? VrUiPreferences.DirectoryPath, FileName);
            return File.Exists(path) && File.ReadAllText(path).Trim() == AcceptedNotice;
        }
        catch { return false; }
    }

    public static bool TryAccept(string? directory = null)
    {
        string? temporary = null;
        try
        {
            string root = directory ?? VrUiPreferences.DirectoryPath;
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, FileName);
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, AcceptedNotice);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
            return IsAccepted(root);
        }
        catch { return false; }
        finally
        {
            try { if (temporary is not null && File.Exists(temporary)) File.Delete(temporary); }
            catch { }
        }
    }

    public static void Enforce(VrTrackingSettings tracking, bool accepted)
    {
        if (accepted) return;
        tracking.ContactBoneTrialEnabled = false;
        tracking.ContactCollisionEnabled = false;
        tracking.ContactHapticsEnabled = false;
        tracking.ContactJiggleEnabled = false;
        tracking.ContactThighElasticEnabled = false;
        tracking.ShowContactLocations = false;
        tracking.ContactShowRanges = false;
    }

    public static bool IsContactSetting(string path) =>
        path.StartsWith("Tracking.Contact", StringComparison.Ordinal) || path == "Tracking.ShowContactLocations";
}
