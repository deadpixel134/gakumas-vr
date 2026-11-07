using System.Globalization;
using GakumasVR.Core;

namespace GakumasVR.Configurator;

internal enum UiLanguage
{
    Korean,
    English,
    Japanese
}

internal static class UiText
{
    private static UiLanguage _lastPersistedLanguage;
    public static UiLanguage CurrentLanguage { get; private set; }

    public static void Initialize()
    {
        VrUiText.Validate();
        CurrentLanguage = (UiLanguage)VrUiPreferences.Load();
        _lastPersistedLanguage = CurrentLanguage;
    }

    public static string Get(string key) => VrUiText.Get((VrUiLanguage)CurrentLanguage, key);

    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public static void SetLanguage(UiLanguage language)
    {
        CurrentLanguage = language;
        if (VrUiPreferences.TrySave((VrUiLanguage)language)) _lastPersistedLanguage = language;
    }

    public static bool ReloadLanguage()
    {
        UiLanguage next = (UiLanguage)VrUiPreferences.Load();
        if (next == _lastPersistedLanguage) return false;
        _lastPersistedLanguage = next;
        CurrentLanguage = next;
        return true;
    }

    public static string Choice(VrHand value) => Get(value switch
    {
        VrHand.Left => "ChoiceLeft",
        VrHand.Right => "ChoiceRight",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    });

    public static string Choice(PanelToggleBinding value) => Get(value switch
    {
        PanelToggleBinding.Grip => "ChoiceGrip",
        PanelToggleBinding.PrimaryFace => "ChoicePrimaryFace",
        PanelToggleBinding.SecondaryFace => "ChoiceSecondaryFace",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    });

    public static string Choice(FaceButtonBinding value) => Get(value switch
    {
        FaceButtonBinding.Primary => "ChoicePrimaryFace",
        FaceButtonBinding.Secondary => "ChoiceSecondaryFace",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    });

    public static string Choice(VrViewTurnMode value) => Get(value switch
    {
        VrViewTurnMode.Smooth => "ChoiceViewTurnSmooth",
        VrViewTurnMode.Snap => "ChoiceViewTurnSnap",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    });

    public static string VisualEffect(string value) => Get(value switch
    {
        VrVisualEffectModes.Approved => "ChoiceVfxApproved",
        VrVisualEffectModes.VrifyFollow => "ChoiceVfxVrifyFollow",
        VrVisualEffectModes.AllOn => "ChoiceVfxAllOn",
        VrVisualEffectModes.AllOff => "ChoiceVfxAllOff",
        VrVisualEffectModes.Manual => "ChoiceVfxManual",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    });

}
