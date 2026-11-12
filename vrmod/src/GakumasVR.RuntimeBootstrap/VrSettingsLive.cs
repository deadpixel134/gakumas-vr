using System.Text.Json;
using System.Text.Json.Nodes;
using GakumasVR.Core;

namespace Doorstop;

internal static partial class VrSettingsRuntime
{
    internal static bool ContactConsentGranted
    {
        get
        {
#if !GAKUMAS_PUBLIC
            return ContactConsent.IsAccepted();
#else
            return false;
#endif
        }
    }

    private static int _menuOpen;
    internal static bool MenuOpen { get => Volatile.Read(ref _menuOpen) != 0; set => Volatile.Write(ref _menuOpen, value ? 1 : 0); }
    private static long _revision = 1;
    private static long _appliedRevision;
    internal static (VrSettings Settings, long Revision) SnapshotSettings()
    {
        lock (Sync) return (_current, _revision);
    }
    internal static long Revision => Interlocked.Read(ref _revision);
    internal static long AppliedRevision => Interlocked.Read(ref _appliedRevision);
    internal static void MarkApplied(long revision) => Interlocked.Exchange(ref _appliedRevision, revision);

    internal static long Publish(VrSettings draft)
    {
        VrSettingsValidationResult result = VrSettingsValidator.Validate(draft);
        if (result.Issues.Count != 0) throw new InvalidOperationException(string.Join(", ", result.Issues));
#if !GAKUMAS_PUBLIC
        ContactConsent.Enforce(result.Settings.Tracking, ContactConsentGranted);
#endif
        lock (Sync)
        {
            _current = result.Settings;
            long revision = Interlocked.Increment(ref _revision);
            return revision;
        }
    }

    internal static void Save(VrSettings settings, string? pathOverride = null)
    {
#if !GAKUMAS_PUBLIC
        settings = VrSettingsValidator.Validate(settings).Settings;
        ContactConsent.Enforce(settings.Tracking, ContactConsentGranted);
#endif
        string path = pathOverride ?? GetSettingsPath(RuntimeProbe.GetLogPath());
        string? previous = File.Exists(path) ? File.ReadAllText(path) : null;
        JsonObject root = previous is null ? new JsonObject() : JsonNode.Parse(previous,
            documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject
            ?? throw new InvalidOperationException("Existing settings are not a JSON object.");
        JsonObject changes = JsonSerializer.SerializeToNode(settings, JsonOptions)!.AsObject();
        Merge(root, changes);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, root.ToJsonString(JsonOptions));
            // Do not silently overwrite edits made by the desktop GUI while this save was prepared.
            if ((File.Exists(path) ? File.ReadAllText(path) : null) != previous)
                throw new IOException("Settings changed externally; reopen the VR menu before saving.");
            if (previous is not null)
                File.Replace(temporary, path, path + ".vr-menu-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".bak");
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void Merge(JsonObject destination, JsonObject source)
    {
        foreach (var property in source)
        {
            if (property.Value is JsonObject nested && destination[property.Key] is JsonObject existing)
                Merge(existing, nested);
            else destination[property.Key] = property.Value is null ? null : JsonNode.Parse(property.Value.ToJsonString());
        }
    }
}
