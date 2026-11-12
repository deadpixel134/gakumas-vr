using System.Globalization;

namespace GakumasVR.Core;

public static class VrSettingsText
{
    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["Runtime.Enabled"] = "RuntimeEnabled",
        ["Render.EyeRenderScale"] = "EyeScale", ["Render.WorldEyeOffsetScale"] = "WorldEyeScale", ["Render.VisualEffectMode"] = "VfxMode",
        ["Render.ManualVisualEffects.PostProcessingEnabled"] = "VfxPostProcessing", ["Render.ManualVisualEffects.VlBloomEnabled"] = "VfxVlBloom",
        ["Render.ManualVisualEffects.VlBloomIntensityScale"] = "VfxVlBloomIntensity", ["Render.ManualVisualEffects.VlBloomDiffusion"] = "VfxVlBloomDiffusion",
        ["Render.ManualVisualEffects.VlDepthOfFieldEnabled"] = "VfxVlDof", ["Render.ManualVisualEffects.VlTextureBlurEnabled"] = "VfxVlTextureBlur",
        ["Render.ManualVisualEffects.VlStarStreakEnabled"] = "VfxVlStarStreak", ["Render.ManualVisualEffects.VlFlareEnabled"] = "VfxVlFlare",
        ["Tracking.LiveSixDofEnabled"] = "LiveSixDof", ["Tracking.LocomotionEnabled"] = "LocomotionEnabled",
        ["Tracking.WorldGripEnabled"] = "WorldGrip",
        ["Tracking.ShowContactHands"] = "ContactHands",
#if !GAKUMAS_PUBLIC
        ["Tracking.ContactBoneTrialEnabled"] = "ContactBoneTrial",
        ["Tracking.ContactCollisionEnabled"] = "ContactCollision",
        ["Tracking.ContactHapticsEnabled"] = "ContactHaptics",
        ["Tracking.ContactJiggleEnabled"] = "ContactJiggle",
        ["Tracking.ContactThighElasticEnabled"] = "ContactThighElastic",
        ["Tracking.ContactThighFrequencyHz"] = "ContactThighFrequencyHz",
        ["Tracking.ContactThighDampingRatio"] = "ContactThighDampingRatio",
        ["Tracking.ContactThighDisplacementMm"] = "ContactThighDisplacementMm",
        ["Tracking.ContactThighTiltDegrees"] = "ContactThighTiltDegrees",
        ["Tracking.ContactThighCompressionPercent"] = "ContactThighCompressionPercent",
        ["Tracking.ContactThighStretchPercent"] = "ContactThighStretchPercent",
        ["Tracking.ContactThighReleasePercent"] = "ContactThighReleasePercent",
        ["Tracking.ShowContactLocations"] = "ContactLocations",
        ["Tracking.ContactShowRanges"] = "ContactRanges",
        ["Tracking.ContactStrengthPercent"] = "ContactStrengthPercent",
        ["Tracking.ContactDurationMultiplier"] = "ContactDurationMultiplier",
        ["Tracking.ContactRangePercent"] = "ContactRangePercent",
        ["Tracking.ContactTiltPercent"] = "ContactTiltPercent",
        ["Tracking.ContactStretchPercent"] = "ContactStretchPercent",
        ["Tracking.ContactHapticStrengthPercent"] = "ContactHapticStrengthPercent",
        ["Tracking.ContactHapticDurationMs"] = "ContactHapticDurationMs",
        ["Tracking.ContactHandRadiusMeters"] = "ContactHandRadiusMeters",
        ["Tracking.ContactBodyRadiusMeters"] = "ContactBodyRadiusMeters",
        ["Tracking.ContactHandHoldSeconds"] = "ContactHandHoldSeconds",
#endif
        ["Tracking.LocomotionHand"] = "LocomotionHand", ["Tracking.LocomotionSpeed"] = "LocomotionSpeed",
        ["Tracking.ViewTurnMode"] = "ViewTurnMode", ["Tracking.ViewTurnSpeed"] = "ViewTurnSpeed", ["Tracking.ViewSnapAngleDegrees"] = "ViewSnapAngle",
        ["Panel.PanelHand"] = "PanelHand", ["Panel.PointerHand"] = "PointerHand", ["Panel.StartEnabled"] = "StartEnabled", ["Panel.ViewerFacing"] = "ViewerFacing",
        ["Panel.OffsetX"] = "OffsetX", ["Panel.OffsetY"] = "OffsetY", ["Panel.OffsetZ"] = "OffsetZ",
        ["Panel.MaximumWidth"] = "MaximumWidth", ["Panel.MaximumHeight"] = "MaximumHeight",
        ["Panel.RotationPitch"] = "RotationPitch", ["Panel.RotationYaw"] = "RotationYaw", ["Panel.RotationRoll"] = "RotationRoll",
        ["Panel.VisibilityHysteresisMilliseconds"] = "VisibilityHysteresis", ["Panel.ToggleBinding"] = "ToggleButton",
        ["Input.PrimaryClickButton"] = "ClickButton", ["Input.BackButton"] = "BackButton", ["Input.TriggerClickEnabled"] = "TriggerEnabled",
        ["Input.ThumbstickScrollEnabled"] = "ScrollEnabled", ["Input.ScrollSensitivity"] = "ScrollSensitivity", ["Input.RequireGameFocus"] = "RequireFocus"
    };

    public static string Label(VrUiLanguage language, string path)
    {
        if (Labels.TryGetValue(path, out string? key)) return VrUiText.Get(language, key);
        if (path.StartsWith("Spatial.", StringComparison.Ordinal))
        {
            key = path.Substring(path.LastIndexOf('.') + 1) switch
            {
                "PerceivedCharacterScale" => "SpatialCharacterWorldSize", "EyeOffsetMode" => "MenuEyeMode", "EyeOffsetMultiplier" => "MenuEyeMultiplier",
                "HeadTranslationMode" => "MenuHeadMode", "HeadTranslationMultiplier" => "MenuHeadMultiplier",
                "LocomotionMode" => "MenuMoveMode", "LocomotionMultiplier" => "MenuMoveMultiplier",
                _ => throw new InvalidOperationException("Untranslated setting: " + path)
            };
            return VrUiText.Get(language, key);
        }
        throw new InvalidOperationException("Untranslated setting: " + path);
    }

    public static string Group(VrUiLanguage language, string group) => VrUiText.Get(language, group switch
    {
        "Runtime" => "MenuRuntime", "Render" => "TabRender", "Render.ManualVisualEffects" => "MenuManualVfxGroup",
        "Tracking" => "MenuTracking", "Panel" => "TabPanel", "Input" => "TabInput",
        "Spatial.Live" => "MenuLiveSpatial", "Spatial.NonLive" => "MenuNonLiveSpatial",
        _ => throw new InvalidOperationException("Untranslated group: " + group)
    });

    public static string Value(VrUiLanguage language, object value, string path = "")
    {
        string? key = value switch
        {
            bool flag => flag ? "MenuOn" : "MenuOff",
            VrHand hand => hand == VrHand.Left ? "ChoiceLeft" : "ChoiceRight",
            PanelToggleBinding binding => binding switch { PanelToggleBinding.Grip => "ChoiceGrip", PanelToggleBinding.PrimaryFace => "MenuPrimary", _ => "MenuSecondary" },
            FaceButtonBinding binding => binding == FaceButtonBinding.Primary ? "MenuPrimary" : "MenuSecondary",
            VrViewTurnMode mode => mode == VrViewTurnMode.Snap ? "ChoiceViewTurnSnap" : "ChoiceViewTurnSmooth",
            SpatialScaleMode mode => mode == SpatialScaleMode.Auto ? "MenuAutomatic" : "MenuManual",
            string mode => mode switch
            {
                VrVisualEffectModes.Approved => "ChoiceVfxApproved", VrVisualEffectModes.VrifyFollow => "ChoiceVfxVrifyFollow",
                VrVisualEffectModes.AllOn => "ChoiceVfxAllOn", VrVisualEffectModes.AllOff => "ChoiceVfxAllOff", VrVisualEffectModes.Manual => "ChoiceVfxManual",
                _ => null
            },
            _ => null
        };
        if (key is not null) return VrUiText.Get(language, key);
        CultureInfo culture = CultureInfo.GetCultureInfo(VrUiPreferences.Code(language));
        if (value is float contactNumber && path.StartsWith("Tracking.Contact", StringComparison.Ordinal) &&
            (path.EndsWith("Meters", StringComparison.Ordinal) || path.EndsWith("Seconds", StringComparison.Ordinal)))
            return contactNumber.ToString("0.000", culture);
        if (value is float number)
            return path.EndsWith("PerceivedCharacterScale", StringComparison.Ordinal) || path.EndsWith("VlBloomIntensityScale", StringComparison.Ordinal)
                ? (number * 100f).ToString("0", culture) + " %" : number.ToString("0.00", culture);
        return Convert.ToString(value, culture) ?? "";
    }
}
