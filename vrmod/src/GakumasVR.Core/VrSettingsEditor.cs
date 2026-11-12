using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GakumasVR.Core;

// Shared schema editor: every editable leaf in the desktop settings model is reachable in VR.
public sealed class VrSettingsEditor
{
    public sealed class Entry
    {
        private readonly object _owner;
        private readonly PropertyInfo _property;
        public string Path { get; }
        public string Label => Regex.Replace(_property.Name, "([a-z])([A-Z])", "$1 $2");
        public string Group => Path.Substring(0, Path.LastIndexOf('.'));
        public string GetLabel(VrUiLanguage language) => VrSettingsText.Label(language, Path);
        public string GetGroup(VrUiLanguage language) => VrSettingsText.Group(language, Group);
        public string GetDisplay(VrUiLanguage language) => VrSettingsText.Value(language, _property.GetValue(_owner)!, Path);
        public bool Unsupported => Path is "Input.ThumbstickScrollEnabled" or "Input.ScrollSensitivity";
        public bool NextSession => Path is "Runtime.Enabled" or "Panel.StartEnabled";
        internal Entry(object owner, PropertyInfo property, string path) { _owner = owner; _property = property; Path = path; }
        public string Display
        {
            get
            {
                object value = _property.GetValue(_owner)!;
                if (value is bool flag) return flag ? "ON" : "OFF";
                if (value is float number) return number.ToString("0.00", CultureInfo.InvariantCulture);
                if (value is string mode) return mode switch
                {
                    VrVisualEffectModes.Approved => "A - APPROVED",
                    VrVisualEffectModes.VrifyFollow => "C - CAMERA FOLLOW",
                    _ => mode.ToUpperInvariant()
                };
                return value.ToString()!.ToUpperInvariant();
            }
        }

        public void Change(int direction)
        {
            if (Unsupported) return;
            object value = _property.GetValue(_owner)!;
            if (value is bool flag) { _property.SetValue(_owner, !flag); return; }
            if (_property.PropertyType.IsEnum)
            {
                Array values = Enum.GetValues(_property.PropertyType);
                int index = Array.IndexOf(values.Cast<object>().ToArray(), value);
                _property.SetValue(_owner, values.GetValue((index + direction + values.Length) % values.Length));
                return;
            }
            if (value is string mode)
            {
                string[] modes = { VrVisualEffectModes.Approved, VrVisualEffectModes.VrifyFollow,
                    VrVisualEffectModes.AllOn, VrVisualEffectModes.AllOff, VrVisualEffectModes.Manual };
                int index = Math.Max(0, Array.IndexOf(modes, mode));
                _property.SetValue(_owner, modes[(index + direction + modes.Length) % modes.Length]);
                return;
            }
            if (Path == "Tracking.ViewSnapAngleDegrees")
            {
                int[] values = { 15, 30, 45, 60 };
                int index = Math.Max(0, Array.IndexOf(values, (int)value));
                _property.SetValue(_owner, values[(index + direction + values.Length) % values.Length]);
                return;
            }
            (float min, float max, float step) = Range();
            float next = Math.Clamp(Convert.ToSingle(value, CultureInfo.InvariantCulture) + direction * step, min, max);
            object converted = value is int ? (object)(int)MathF.Round(next) : MathF.Round(next, 3);
            _property.SetValue(_owner, converted);
        }

        private (float, float, float) Range() => _property.Name switch
        {
            "EyeRenderScale" => (.5f, 2f, .05f),
            "WorldEyeOffsetScale" => (0f, .5f, .025f),
            "VlBloomIntensityScale" => (0f, 3f, .1f),
            "VlBloomDiffusion" => (1f, 10f, 1f),
            "LocomotionSpeed" => (.1f, 5f, .05f),
            "ViewTurnSpeed" => (15f, 180f, 5f),
            "PerceivedCharacterScale" => (.1f, 4f, .05f),
            "OffsetX" or "OffsetY" or "OffsetZ" => (-.5f, .5f, .01f),
            "MaximumWidth" or "MaximumHeight" => (.1f, 1f, .025f),
            "RotationPitch" or "RotationYaw" or "RotationRoll" => (-180f, 180f, 5f),
            "VisibilityHysteresisMilliseconds" => (0f, 500f, 25f),
            "ScrollSensitivity" => (.1f, 5f, .1f),
#if !GAKUMAS_PUBLIC
            "ContactStrengthPercent" => (0f, 300f, 5f),
            "ContactThighFrequencyHz" => (2f, 8f, 0.1f),
            "ContactThighDampingRatio" => (0.3f, 1.5f, 0.01f),
            "ContactThighDisplacementMm" => (0f, 12f, 0.5f),
            "ContactThighTiltDegrees" => (0f, 4f, 0.1f),
            "ContactThighCompressionPercent" => (0f, 5f, 0.1f),
            "ContactThighStretchPercent" => (0f, 5f, 0.1f),
            "ContactThighReleasePercent" => (0f, 20f, 1f),
            "ContactDurationMultiplier" => (0.1f, 3f, 0.05f),
            "ContactRangePercent" => (0f, 200f, 5f),
            "ContactTiltPercent" => (0f, 200f, 5f),
            "ContactStretchPercent" => (0f, 200f, 5f),
            "ContactHapticStrengthPercent" => (0f, 100f, 5f),
            "ContactHapticDurationMs" => (0f, 250f, 5f),
            "ContactHandRadiusMeters" => (0.01f, 0.1f, 0.005f),
            "ContactBodyRadiusMeters" => (0.015f, 0.15f, 0.005f),
            "ContactHandHoldSeconds" => (0.025f, 0.25f, 0.025f),
#endif
            _ => (0f, 4f, .05f)
        };
    }

    public VrSettings Draft { get; }
    public IReadOnlyList<Entry> Entries { get; }
    public VrSettingsEditor(VrSettings settings) : this(settings, true) { }

    public VrSettingsEditor(VrSettings settings, bool includeContactSettings)
    {
        Draft = VrSettingsValidator.Validate(settings).Settings;
        List<Entry> entries = new();
        Add(Draft, "", entries);
#if !GAKUMAS_PUBLIC
        if (!includeContactSettings)
        {
            ContactConsent.Enforce(Draft.Tracking, accepted: false);
            entries.RemoveAll(entry => ContactConsent.IsContactSetting(entry.Path));
        }
#endif
        // The comparison profile is always on the first page.
        entries.Sort((a, b) => a.Path == b.Path ? 0 : a.Path == "Render.VisualEffectMode" ? -1 : b.Path == "Render.VisualEffectMode" ? 1 :
            string.Compare(a.Path, b.Path, StringComparison.Ordinal));
        Entries = entries;
    }
    public void Change(int index, int direction)
    {
        Entries[index].Change(direction);
        if (Entries[index].Path.StartsWith("Render.ManualVisualEffects.", StringComparison.Ordinal))
            Draft.Render.VisualEffectMode = VrVisualEffectModes.Manual;
        if (Draft.Panel.PanelHand == Draft.Panel.PointerHand)
        {
            if (Entries[index].Path == "Panel.PanelHand") Draft.Panel.PointerHand = Draft.Panel.PanelHand == VrHand.Left ? VrHand.Right : VrHand.Left;
            else Draft.Panel.PanelHand = Draft.Panel.PointerHand == VrHand.Left ? VrHand.Right : VrHand.Left;
        }
        if (Draft.Input.PrimaryClickButton == Draft.Input.BackButton)
        {
            if (Entries[index].Path == "Input.PrimaryClickButton") Draft.Input.BackButton = Draft.Input.PrimaryClickButton == FaceButtonBinding.Primary ? FaceButtonBinding.Secondary : FaceButtonBinding.Primary;
            else Draft.Input.PrimaryClickButton = Draft.Input.BackButton == FaceButtonBinding.Primary ? FaceButtonBinding.Secondary : FaceButtonBinding.Primary;
        }
    }
    private static void Add(object owner, string prefix, List<Entry> entries)
    {
        foreach (PropertyInfo property in owner.GetType().GetProperties())
        {
            if (!property.CanRead || !property.CanWrite || property.Name == "SchemaVersion") continue;
            string path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            object value = property.GetValue(owner)!;
            if (value is bool or int or float or string || property.PropertyType.IsEnum)
                entries.Add(new Entry(owner, property, path));
            else Add(value, path, entries);
        }
    }
}
